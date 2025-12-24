using System.Text.Json;
using System.Text.Json.Serialization;
using Gozon.Contracts.Messaging;
using Gozon.Orders.Data;
using Gozon.Orders.Domain;
using Gozon.Orders.Infrastructure;
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

builder.Services.AddDbContext<OrdersDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("OrdersDb");
    opt.UseNpgsql(cs);
});

// HealthChecks: /health (жив), /ready (готов, БД доступна)
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("OrdersDb")!);

builder.Services.Configure<Gozon.Orders.Messaging.RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddSingleton<Gozon.Orders.Messaging.RabbitMqConnectionFactory>();

builder.Services.AddHostedService<DbMigratorHostedService<OrdersDbContext>>();
builder.Services.AddHostedService<Gozon.Orders.Messaging.OrdersOutboxPublisher>();
builder.Services.AddHostedService<Gozon.Orders.Messaging.OrdersPaymentsEventsConsumer>();

var app = builder.Build();

// Swagger доступен даже если БД недоступна
app.UseSwagger();
app.UseSwaggerUI();

// Middleware: если БД недоступна — возвращаем 503 на бизнес-эндпоинты
app.UseMiddleware<DbAvailabilityMiddleware<OrdersDbContext>>();

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

// Создать заказ + записать событие в outbox одной транзакцией
app.MapPost("/orders", async (HttpRequest req, OrdersDbContext db, CreateOrderRequest body) =>
{
    var userId = GetUserId(req);

    if (body.Amount <= 0)
        return Results.BadRequest("Сумма должна быть больше 0");

    var orderId = Guid.NewGuid();
    var now = DateTimeOffset.UtcNow;

    var order = new Order
    {
        Id = orderId,
        UserId = userId,
        Amount = body.Amount,
        Description = body.Description,
        Status = OrderStatus.New,
        CreatedAtUtc = now
    };

    var evt = new OrderPaymentRequested(
        MessageId: Guid.NewGuid(),
        OrderId: orderId,
        UserId: userId,
        Amount: body.Amount,
        OccurredAtUtc: now
    );

    var outbox = new OutboxMessage
    {
        Id = Guid.NewGuid(),
        Type = nameof(OrderPaymentRequested),
        PayloadJson = JsonSerializer.Serialize(evt),
        OccurredAtUtc = now,
        Attempts = 0
    };

    await using var tx = await db.Database.BeginTransactionAsync();
    db.Orders.Add(order);
    db.OutboxMessages.Add(outbox);
    await db.SaveChangesAsync();
    await tx.CommitAsync();

    return Results.Accepted($"/orders/{orderId}", new { orderId });
});

app.MapGet("/orders", async (HttpRequest req, OrdersDbContext db) =>
{
    var userId = GetUserId(req);

    var orders = await db.Orders
        .Where(x => x.UserId == userId)
        .OrderByDescending(x => x.CreatedAtUtc)
        .Select(x => new
        {
            x.Id,
            x.Amount,
            x.Description,
            Status = x.Status.ToString(),
            x.CreatedAtUtc
        })
        .ToListAsync();

    return Results.Ok(orders);
});

app.MapGet("/orders/{id:guid}", async (HttpRequest req, OrdersDbContext db, Guid id) =>
{
    var userId = GetUserId(req);

    var order = await db.Orders.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
    if (order is null)
        return Results.NotFound();

    return Results.Ok(new
    {
        order.Id,
        order.Amount,
        order.Description,
        Status = order.Status.ToString(),
        order.CreatedAtUtc
    });
});

app.Run();

public sealed record CreateOrderRequest(decimal Amount, string? Description);
