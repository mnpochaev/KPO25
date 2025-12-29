# Гоzон — ДЗ №4. Асинхронное межсервисное взаимодействие

Проект — микросервисная система интернет-магазина «Гоzон» с **асинхронной автооплатой заказов** через брокер сообщений.

---

## Архитектура

Система состоит из 5 контейнеров (в docker-compose):

1. **Gozon.Gateway** — API Gateway (routing HTTP) + WebSocket-уведомления о статусах заказов  
2. **Gozon.Orders** — заказы + Transactional Outbox + обработка событий оплаты  
3. **Gozon.Payments** — счета/баланс + Transactional Inbox/Outbox + списание денег  
4. **orders-db** — PostgreSQL для Orders  
5. **payments-db** — PostgreSQL для Payments  
+ **rabbitmq** — брокер сообщений

Схема взаимодействия:

```

Client / Frontend
│ HTTP + WS
▼
Gateway (5241)  ──────────────► Orders (5242) ─────► orders-db
│                                  │
│                                  │  (Transactional Outbox)
│                                  ▼
│                              RabbitMQ (topic exchange: "gozon")
│                                  ▲
│                                  │  (Transactional Inbox + Outbox)
▼                                  │
WebSocket notifications ◄────────── Payments (5243) ───► payments-db

````

### Топики (routing keys) RabbitMQ

Используется `topic exchange` **gozon**:

- `order.payment.requested` — Orders → Payments (нужно списать деньги)
- `payment.succeeded` — Payments → Orders (оплата успешна)
- `payment.failed` — Payments → Orders (оплата не удалась)
- `order.status.changed` — Orders → Gateway (для WebSocket-уведомлений)

---

## Быстрый старт (Docker)

Из корня репозитория:

```bash
docker compose up --build
````

### Порты

* Gateway: `http://localhost:5241`
* Orders: `http://localhost:5242`
* Payments: `http://localhost:5243`
* Frontend: `http://localhost:3000`
* RabbitMQ Management: `http://localhost:15672` (логин/пароль: `guest/guest`)
* Postgres Orders (наружу): `localhost:5433`
* Postgres Payments (наружу): `localhost:5434`

### Swagger

* Gateway Swagger: `http://localhost:5241/swagger`
* Orders Swagger: `http://localhost:5242/swagger`
* Payments Swagger: `http://localhost:5243/swagger`

---

## Общие правила API

Во **все HTTP-запросы** нужно передавать заголовок:

* `X-User-Id: <long>`

Без него сервисы вернут ошибку.


---

## Внешнее API (через Gateway)

Gateway проксирует запросы в нужный сервис. Маршруты у Gateway и у сервисов одинаковые.

### Accounts / Payments

#### 1) Создать счёт (у пользователя максимум один)

`POST /accounts`

```bash
curl -X POST "http://localhost:5241/accounts" \
  -H "X-User-Id: 1"
```

#### 2) Пополнить счёт

`POST /accounts/topup`

Body:

```json
{ "amount": 1500 }
```

```bash
curl -X POST "http://localhost:5241/accounts/topup" \
  -H "Content-Type: application/json" \
  -H "X-User-Id: 1" \
  -d '{ "amount": 1500 }'
```

#### 3) Посмотреть баланс

`GET /accounts/balance`

```bash
curl "http://localhost:5241/accounts/balance" \
  -H "X-User-Id: 1"
```

---

### Orders

#### 1) Создать заказ (асинхронная автооплата)

`POST /orders`

Body:

```json
{ "amount": 499.99, "description": "Подарок" }
```

Ответ: `202 Accepted` + `orderId`

```bash
curl -X POST "http://localhost:5241/orders" \
  -H "Content-Type: application/json" \
  -H "X-User-Id: 1" \
  -d '{ "amount": 499.99, "description": "Подарок" }'
```

#### 2) Список заказов

`GET /orders`

```bash
curl "http://localhost:5241/orders" \
  -H "X-User-Id: 1"
```

#### 3) Статус/детали заказа

`GET /orders/{id}`

```bash
curl "http://localhost:5241/orders/<ORDER_ID>" \
  -H "X-User-Id: 1"
```

---

## WebSocket (live-обновление статуса заказа)

Gateway принимает WS-подключения по адресу:

* `ws://localhost:5241/ws?userId=<id>`

Пример (wscat):

```bash
wscat -c "ws://localhost:5241/ws?userId=1"
```

Формат push-сообщения (JSON), которое приходит клиенту при смене статуса заказа:

```json
{
  "status": "Finished",
  "orderId": "....",
  "occurredAtUtc": "...."
}
```

---

## Frontend

Откройте в браузере:

* `http://localhost:3000`

Функции:

* выбрать `X-User-Id`
* создать счёт / пополнить / посмотреть баланс
* создать заказ
* видеть список заказов
* получать toast-уведомления по WebSocket при смене статуса

---

## Внутреннее взаимодействие и надёжность

### Ключевой сценарий: создание заказа и автооплата

1. **Client → Orders**: `POST /orders`
   Orders в **одной транзакции** сохраняет:

    * `orders` (заказ со статусом `New`)
    * `outbox_messages` (событие `OrderPaymentRequested`)

2. **Orders Outbox Publisher** читает `outbox_messages` и публикует в RabbitMQ (`order.payment.requested`) с publisher confirms.

3. **Payments Consumer** читает сообщение и делает **Transactional Inbox**:

    * проверка `inbox_messages` по `MessageId` (дедуп)
    * доп. защита: `payments` имеет уникальность по `OrderId` (чтобы не создать повторный Payment)

4. **Payments** пытается списать деньги атомарно (CAS на балансе):

    * `UPDATE accounts SET Balance = Balance - amount WHERE UserId = ... AND Balance >= amount`
    * если успех → `PaymentSucceeded`
    * если нет счёта / недостаточно средств → `PaymentFailed`
      Результат записывается в `payments` и событие кладётся в `payments.outbox_messages`.

5. **Payments Outbox Publisher** публикует `payment.succeeded` / `payment.failed`.

6. **Orders Consumer** читает события оплаты и обновляет `orders.status`:

    * success → `Finished`
    * fail → `Cancelled`
      Событие дедуплицируется через `orders.inbox_messages`.
      Дополнительно Orders кладёт `OrderStatusChanged` в свой outbox → Gateway получает WS-событие.

---

## Retry / Backoff / DLQ

* **Consumers** используют ограниченное число попыток с экспоненциальной задержкой:

    * Payments consumer: `maxAttempts = 3`
    * Orders consumer и Gateway notifications listener: `maxAttempts = 5`
* При превышении лимита (в Orders/Gateway) сообщение **nack**’ается с `requeue=false` и уходит в **DLQ** (`*.dlq`) через DLX.
* **Outbox publishers** делают retry публикации на уровне БД-сообщений (поле `Attempts` + задержка), публикуют только после broker confirm.

---

## Хранение данных (PostgreSQL)

### Orders DB (`orders-db`)

Основные таблицы:

* `orders` — (Id, UserId, Amount, Description, Status, CreatedAtUtc)
* `outbox_messages` — transactional outbox (события в RabbitMQ)
* `inbox_messages` — дедуп входящих событий оплаты (MessageId)

### Payments DB (`payments-db`)

Основные таблицы:

* `accounts` — (UserId, Balance)
* `payments` — фиксация факта оплаты (уникальность по OrderId)
* `inbox_messages` — transactional inbox (дедуп OrderPaymentRequested)
* `outbox_messages` — transactional outbox (PaymentSucceeded/PaymentFailed)

---

## Health endpoints

У сервисов доступны:

* `/health` — liveness
* `/ready` — readiness (для Orders/Payments проверяет доступность БД)

---

## Локальный запуск без Docker

Нужно поднять RabbitMQ + Postgres (можно через docker-compose), затем:

```bash
dotnet run --project src/Gozon.Orders
dotnet run --project src/Gozon.Payments
dotnet run --project src/Gozon.Gateway
dotnet run --project src/Gozon.Frontend
```

---

## Проверка сценария

1. Создать счёт: `POST /accounts`
2. Пополнить: `POST /accounts/topup { amount: 1000 }`
3. Создать заказ на сумму <= баланса: `POST /orders`
4. Подождать пару секунд и проверить статус: `GET /orders/{id}`
5. Открыть WS (`/ws?userId=...`) — увидеть push о смене статуса.

