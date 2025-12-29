using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Gozon.Payments.Data;

/// <summary>
/// Нужен для того, чтобы dotnet-ef мог создавать DbContext при генерации миграций.
/// </summary>
public sealed class PaymentsDbContextFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    public PaymentsDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = config.GetConnectionString("PaymentsDb")
                 ?? throw new InvalidOperationException("Не найдена строка подключения ConnectionStrings:PaymentsDb");

        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseNpgsql(cs)
            .Options;

        return new PaymentsDbContext(options);
    }
}