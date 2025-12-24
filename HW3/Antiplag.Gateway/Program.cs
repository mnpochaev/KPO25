using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Http.Resilience;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger (OpenAPI)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Antiplag – API Gateway",
        Version = "v1",
        Description = "Центральная точка входа. Маршрутизирует запросы к FileStorage и Analysis."
    });
});

// HttpClient для FileStorage
builder.Services.AddHttpClient("fileStorage", client =>
    {
        // BaseUrl из конфигурации (с фолбэком на локальный)
        var baseUrl =
            builder.Configuration["Services:FileStorage"]
            ?? builder.Configuration["FileStorage:BaseUrl"]
            ?? "http://localhost:5235";

        client.BaseAddress = new Uri(baseUrl);
    })
    // Таймауты/ретраи/circuit breaker
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

// HttpClient для Analysis
builder.Services.AddHttpClient("analysis", client =>
    {
        // BaseUrl из конфигурации (с фолбэком на локальный)
        var baseUrl =
            builder.Configuration["Services:Analysis"]
            ?? builder.Configuration["Analysis:BaseUrl"]
            ?? "http://localhost:5180";

        client.BaseAddress = new Uri(baseUrl);
    })
    // Таймауты/ретраи/circuit breaker
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Antiplag – Gateway v1");
        c.DocumentTitle = "Antiplag – Gateway API";
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        c.DefaultModelsExpandDepth(-1);
    });
}

app.UseRouting();
app.UseAuthorization();

// Раздача статики (например, index.html) и файлов по умолчанию
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.Run();