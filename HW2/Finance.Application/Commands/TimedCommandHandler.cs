using System.Diagnostics;
using Finance.Application.Metrics;

namespace Finance.Application.Commands;

/// <summary>
/// Декоратор, измеряющий время выполнения сценария.
/// </summary>
public sealed class TimedCommandHandler<TCommand, TResult> : ICommandHandler<TCommand, TResult>
{
    private readonly ICommandHandler<TCommand, TResult> _inner;
    private readonly IScenarioMetrics _metrics;

    public TimedCommandHandler(ICommandHandler<TCommand, TResult> inner, IScenarioMetrics metrics)
    {
        _inner = inner;
        _metrics = metrics;
    }

    public string Name => _inner.Name;

    public TResult Handle(TCommand command)
    {
        var sw = Stopwatch.StartNew();
        var result = _inner.Handle(command);
        sw.Stop();
        _metrics.Add(Name, sw.ElapsedMilliseconds);
        return result;
    }
}
