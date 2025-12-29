# Antiplag — ДЗ №3. Микросервисный антиплагиат

Проект — микросервисная система для:

* загрузки файлов контрольных работ,
* хранения файлов,
* синхронного анализа на антиплагиат,
* выдачи отчётов/аналитики по конкретной работе.

---

## Архитектура

Система состоит из 3 сервисов:

1. **Antiplag.Gateway** (API Gateway)
2. **Antiplag.FileStorage** (хранение/выдача файлов)
3. **Antiplag.Analysis** (анализ, отчёты, Postgres)

Схема взаимодействия:

```
Client
  │
  ▼
Gateway (8000)
  │
  ├──► FileStorage (8001)  — сохранить/выдать файл
  │
  └──► Analysis   (8002)   — анализ + отчёты (Postgres)
            │
            ▼
         Postgres
```

---

## Быстрый старт (Docker)


### Запуск

```bash
docker compose up --build
```

### Порты (docker-compose)

* Gateway: `http://localhost:8000`
* FileStorage: `http://localhost:8001`
* Analysis: `http://localhost:8002`
* Postgres: внутри docker network (volume `postgres_data`)

### Swagger

* Gateway Swagger: `http://localhost:8000/swagger`
* FileStorage Swagger: `http://localhost:8001/swagger`
* Analysis Swagger: `http://localhost:8002/swagger`

---

## Внешнее API (через Gateway)

### 1) Отправить работу на проверку

`POST /api/v1/submissions` (multipart/form-data)

Поля формы:

* `file` — файл работы
* `workId` — идентификатор контрольной/задания
* `studentId` — идентификатор студента
* `studentName` — имя студента (возвращается в ответе)
* `group` — группа (опционально)

Ответ: включает `submissionId`, `fileId` и результаты анализа (`plagiarism`, `similarity`, `originalSubmissionId`, `originalStudentId`, `wordCloudUrl`).

Пример curl:

```bash
curl -X POST "http://localhost:8000/api/v1/submissions" \
  -F "file=@./work.pdf" \
  -F "workId=CONTROL_1" \
  -F "studentId=12345" \
  -F "studentName=Ivan Ivanov" \
  -F "group=221"
```

---

### 2) Получить отчёты по конкретной работе (для преподавателя)

`GET /api/v1/works/{workId}/reports`

Возвращает агрегированную сводку + список отчётов по всем сдачам указанного `workId`:

* `totalSubmissions`
* `plagiarizedCount`
* `plagiarizedShare`
* `averageSimilarity`
* `maxSimilarity`
* `uniqueStudents`
* `firstSubmissionAt` / `lastSubmissionAt`
* `items[]` (по каждой сдаче)

---

### 3) Скачать оригинальный файл по fileId

`GET /api/v1/files/{fileId}`

Gateway проксирует скачивание из FileStorage и отдаёт файл с корректными заголовками.

---

## Внутреннее API (межсервисное)

### FileStorage

* `POST /internal/files` — сохранить файл, вернуть `fileId` (+ метаданные ответа)
* `GET /internal/files/{fileId}` — отдать файл (контент + корректный Content-Type + имя `{guid}.{ext}`)

### Analysis

* `POST /internal/analysis/submissions` — выполнить анализ, сохранить в Postgres, вернуть отчёт
* `GET /internal/analysis/works/{workId}/reports` — сводка по workId из Postgres

---

## Хранение данных

### FileStorage

* хранит файлы **на диске** (в контейнере) в директории `files/`
* в docker используется volume: `filestorage_data:/app/files`
* файл сохраняется как `{fileId}{original_extension}` (например: `{guid}.pdf`)

### Analysis (PostgreSQL)

В Postgres хранится:

* факт сдачи (`submissions`) — `submissionId`, `workId`, `studentId`, `fileId`, `hash`, `shingles[]`, `createdAt`
* отчёт (`reports`) — `reportId`, `submissionId`, `plagiarism`, `similarity`, `originalSubmissionId`, `originalStudentId`, `wordCloudUrl`, `createdAt`

На старте Analysis делает `EnsureCreated()` с ретраями, чтобы дождаться готовности Postgres.

---

## Алгоритм определения плагиата

Ключевые параметры:

* длина шингла: **5 слов**
* порог плагиата: **0.5** (50%)

### Шаг 1. Получение файла

Analysis по `fileId` синхронно скачивает файл из FileStorage:
`GET /internal/files/{fileId}`

### Шаг 2. Извлечение текста (поддерживаемые форматы)

Analysis пытается извлечь текст из:

* **PDF** (по сигнатуре `%PDF`, через PdfPig)
* **DOCX/PPTX** (по сигнатуре ZIP `PK`, через OpenXML; сначала пробует DOCX, затем PPTX)
* **text** (UTF-8)

Если текст не извлечён — будет пустая строка, шинглы будут пустыми/минимальными.

### Шаг 3. “Полное совпадение”

Считается `SHA-256` **по байтам файла** (не по тексту).

Если среди более ранних сдач **того же `workId`** от **других студентов** находится запись с таким же `hash`, то:

* `plagiarism = true`
* `similarity = 1.0`
* `originalSubmissionId` и `originalStudentId` берутся от **самой ранней** совпавшей сдачи.

### Шаг 4. “Частичное совпадение” (шинглы + Jaccard)

1. Текст нормализуется:

* `ToLowerInvariant()`
* токенизация: split по `\W+` (не-словесные символы), пустые токены удаляются

2. Строятся шинглы размера 5 слов:

* каждый шингл — строка из 5 подряд идущих токенов
* шинглы хранятся **как множество** (без частот), чтобы далее применять Jaccard

3. Для каждой прошлой сдачи (тот же `workId`, другой `studentId`) считается **Jaccard similarity**:
   [
   |A ∩ B| / |A ∪ B|
   ]

Берётся **лучшее** совпадение (`bestSimilarity`).

### Шаг 5. Правило принятия решения

* если было полное совпадение по hash → плагиат
* иначе если `bestSimilarity >= 0.5` → плагиат
* иначе → не плагиат, но `similarity` всё равно возвращается как максимальная найденная схожесть

---

## Word Cloud

В отчёт добавляется `wordCloudUrl` — ссылка на QuickChart Word Cloud.

Как формируется:

* текст очищается (оставляются буквы/цифры/пробелы и `,.;:!?-_`)
* пробелы нормализуются
* длина ограничивается **5000 символами**
* строится URL вида:

`https://quickchart.io/wordcloud?format=png&width=600&height=600&text=...`

(картинка PNG формируется на стороне QuickChart по этому URL)

---

## Устойчивость и обработка ошибок

* **Gateway** и **Analysis** используют `HttpClient` с resilience-политиками:

    * attempt timeout
    * total request timeout
    * retry (2 попытки)
    * circuit breaker
* Если FileStorage/Analysis недоступны, Gateway возвращает `503 Service Unavailable`.
* Analysis при недоступности Postgres/ошибках запросов возвращает `503`.
---
## Мини-UI (страницы для удобства)

Для удобной ручной проверки без Swagger в **Antiplag.Gateway** добавлены две статические страницы:

* **`index.html`** — стартовая страница (студентская форма/загрузка)
* **`teacher.html`** — страница преподавателя (просмотр отчётов по `workId`)

### Адреса


* `http://localhost:8000/` (это `index.html` по умолчанию)
* `http://localhost:8000/teacher.html`

