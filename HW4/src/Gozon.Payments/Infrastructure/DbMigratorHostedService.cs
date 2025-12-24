using Microsoft.EntityFrameworkCore;

namespace Gozon.Payments.Infrastructure;

/// <summary>
/// Запускает миграции в фоне с ретраями, не валит процесс, если БД временно недоступна.
/// </summary>
public sealed class DbMigratorHostedService<TDbContext> : IHostedService where TDbContext : DbContext
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<DbMigratorHostedService<TDbContext>> _logger;

    public DbMigratorHostedService(
        IServiceProvider sp,
        ILogger<DbMigratorHostedService<TDbContext>> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(() => RunAsync(cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(1);
        const int maxAttempts = 10;

        for (var attempt = 1; attempt <= maxAttempts && !cancellationToken.IsCancellationRequested; attempt++)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
                await db.Database.MigrateAsync(cancellationToken);

                _logger.LogInformation("Миграции базы применены успешно");
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось применить миграции (попытка {Attempt}/{Max}).", attempt, maxAttempts);
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30));
            }
        }

        _logger.LogError("Миграции не применены после {Max} попыток", maxAttempts);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
