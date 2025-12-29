using System;
using Finance.Application.Export;
using Finance.Application.Logging;
using Finance.Application.Operations.Filters;

namespace Finance.Application.Decorators;

/// <summary>
/// Декоратор, добавляющий логирование к сервису экспорта (Decorator pattern).
/// </summary>
public sealed class LoggingExportServiceDecorator : IExportService
{
    private readonly IExportService _inner;
    private readonly ILogSink _logSink;

    public LoggingExportServiceDecorator(IExportService inner, ILogSink logSink)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logSink = logSink ?? throw new ArgumentNullException(nameof(logSink));
    }

    public string Export(OperationQuery? query, DataFormat format)
    {
        var result = _inner.Export(query, format);
        _logSink.Log($"Экспорт выполнен: формат {format}, время {DateTime.UtcNow:O}");
        return result;
    }
}
