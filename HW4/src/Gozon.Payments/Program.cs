using Gozon.Payments.Data;
using Gozon.Payments.Domain;
using Gozon.Payments.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.AddSecurityDefinition("X-User-Id", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-User-Id",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Description = "UserId для вызовов API"
    });
    opt.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "X-User-Id"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("PaymentsDb")!);

builder.Services.AddDbContext<PaymentsDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("PaymentsDb");
    opt.UseNpgsql(cs);
});

builder.Services.Configure<Gozon.Payments.Messaging.RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddSingleton<Gozon.Payments.Messaging.RabbitMqConnectionFactory>();

builder.Services.AddHostedService<DbMigratorHostedService<PaymentsDbContext>>();
builder.Services.AddHostedService<Gozon.Payments.Messaging.PaymentsOrdersConsumer>();
builder.Services.AddHostedService<Gozon.Payments.Messaging.PaymentsOutboxPublisher>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Если БД недоступна — на бизнес-эндпоинты отдаём 503 вместо падения процесса
app.UseMiddleware<DbAvailabilityMiddleware<PaymentsDbContext>>();

// Health endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/ready");

static long GetUserId(HttpRequest request)
{
    if (!request.Headers.TryGetValue("X-User-Id", out var values))
        throw new InvalidOperationException("Отсутствует заголовок X-User-Id");

    if (!long.TryParse(values.ToString(), out var userId) || userId <= 0)
        throw new InvalidOperationException("Некорректный X-User-Id");

    return userId;
}

// 1) Создать счёт (если уже есть — вернуть текущий)
app.MapPost("/accounts", async (HttpRequest req, PaymentsDbContext db) =>
{
    var userId = GetUserId(req);

    var existing = await db.Accounts.FirstOrDefaultAsync(x => x.UserId == userId);
    if (existing is not null)
        return Results.Ok(new { existing.UserId, existing.Balance });

    var acc = new Account { UserId = userId, Balance = 0m };
    db.Accounts.Add(acc);
    await db.SaveChangesAsync();

    return Results.Created("/accounts/balance", new { acc.UserId, acc.Balance });
});

// 2) Пополнить счёт
app.MapPost("/accounts/topup", async (HttpRequest req, PaymentsDbContext db, TopUpRequest body) =>
{
    var userId = GetUserId(req);

    if (body.Amount <= 0)
        return Results.BadRequest("Сумма должна быть больше 0");

    // Атомарное пополнение, чтобы не было гонок с одновременными списаниями/пополнениями.
    var rows = await db.Database.ExecuteSqlInterpolatedAsync(
        $"UPDATE accounts SET \"Balance\" = \"Balance\" + {body.Amount} WHERE \"UserId\" = {userId}",
        req.HttpContext.RequestAborted);

    if (rows == 0)
        return Results.NotFound("Счёт не найден. Создайте его сначала.");

    // Читаем баланс после обновления, чтобы вернуть актуальное значение.
    var acc = await db.Accounts.FirstOrDefaultAsync(x => x.UserId == userId, req.HttpContext.RequestAborted);
    return Results.Ok(new { acc!.UserId, acc.Balance });
});

// 3) Баланс
app.MapGet("/accounts/balance", async (HttpRequest req, PaymentsDbContext db) =>
{
    var userId = GetUserId(req);

    var acc = await db.Accounts.FirstOrDefaultAsync(x => x.UserId == userId);
    if (acc is null)
        return Results.NotFound("Счёт не найден. Создайте его сначала.");

    return Results.Ok(new { acc.UserId, acc.Balance });
});

app.Run();

public sealed record TopUpRequest(decimal Amount);
