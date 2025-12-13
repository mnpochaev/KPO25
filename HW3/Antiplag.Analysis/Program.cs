using Antiplag.Analysis.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger (OpenAPI)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Antiplag.Analysis API",
        Version = "v1",
        Description = "Сервис анализа студенческих работ на плагиат. Получает файлы из FileStorage и сохраняет результаты в Postgres."
    });
});

// ---- Database (PostgreSQL) ----
// Берём connection string из конфигурации, иначе дефолт для локалки
var connectionString = builder.Configuration.GetConnectionString("AnalysisDb")
                       ?? "Host=localhost;Port=5432;Database=antiplag;Username=antiplag;Password=antiplag";

builder.Services.AddDbContext<AnalysisDbContext>(options =>
{
    options.UseNpgsql(connectionString);

    // В dev — больше деталей для отладки
    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
    }
});

// ---- HTTP clients ----
// Клиент для общения с FileStorage
builder.Services.AddHttpClient("fileStorage", client =>
    {
        // BaseUrl из конфигурации (с фолбэком на локальный)
        var baseUrl =
            builder.Configuration["Services:FileStorage"]
            ?? builder.Configuration["FileStorage:BaseUrl"]
            ?? "http://localhost:5235";

        client.BaseAddress = new Uri(baseUrl);
    })
    // Таймауты/ретраи/circuit breaker для устойчивости
    .AddStandardResilienceHandler(o =>
    {
        o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
        o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(8);

        o.Retry.MaxRetryAttempts = 2;
        o.Retry.Delay = TimeSpan.FromMilliseconds(200);

        o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        o.CircuitBreaker.MinimumThroughput = 8;
        o.CircuitBreaker.FailureRatio = 0.5;
        o.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
    });

var app = builder.Build();

// Ждём Postgres и создаём схему при старте
await using (var scope = app.Services.CreateAsyncScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbBootstrap");
    var db = scope.ServiceProvider.GetRequiredService<AnalysisDbContext>();

    const int maxAttempts = 10;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "Postgres is not ready yet (attempt {Attempt}/{Max}). Retrying...", attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Antiplag.Analysis v1");
        c.DocumentTitle = "Antiplag – Analysis API";
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        c.DefaultModelsExpandDepth(-1);
    });
}

app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.Run();
