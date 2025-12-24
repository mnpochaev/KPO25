using System.Net.Http.Headers;
using Gozon.Contracts.Messaging;
using Gozon.Gateway;
using Gozon.Gateway.Messaging;

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
builder.Services.AddHealthChecks();
builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials().WithOrigins(
            "http://localhost:3000",
            "http://127.0.0.1:3000",
            "http://localhost:5241",
            "http://127.0.0.1:5241");
    });
});

builder.Services.AddHttpClient("orders", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:Orders"] ?? "http://localhost:5242");
});

builder.Services.AddHttpClient("payments", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:Payments"] ?? "http://localhost:5243");
});

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<RabbitMqConnectionFactory>();
builder.Services.AddSingleton<WebSocketNotifier>();
builder.Services.AddHostedService<PaymentsEventsListener>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseWebSockets();

app.MapHealthChecks("/health");
app.MapHealthChecks("/ready");

app.MapPost("/orders", Proxy(HttpMethod.Post, "orders", _ => "/orders", "Orders"));
app.MapGet("/orders", Proxy(HttpMethod.Get, "orders", _ => "/orders", "Orders"));
app.MapGet("/orders/{id:guid}", Proxy(HttpMethod.Get, "orders", ctx => $"/orders/{ctx.Request.RouteValues["id"]}", "Orders"));

app.MapPost("/accounts", Proxy(HttpMethod.Post, "payments", _ => "/accounts", "Payments"));
app.MapPost("/accounts/topup", Proxy(HttpMethod.Post, "payments", _ => "/accounts/topup", "Payments"));
app.MapGet("/accounts/balance", Proxy(HttpMethod.Get, "payments", _ => "/accounts/balance", "Payments"));

app.Map("/ws", async (HttpContext ctx, WebSocketNotifier notifier) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        await ctx.Response.WriteAsync("Это endpoint для WebSocket");
        return;
    }

    if (!ctx.Request.Query.TryGetValue("userId", out var raw) || !long.TryParse(raw, out var userId) || userId <= 0)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        await ctx.Response.WriteAsync("Нужно передать userId в query");
        return;
    }

    using var socket = await ctx.WebSockets.AcceptWebSocketAsync();
    notifier.Add(userId, socket);

    var buffer = new byte[1024];
    while (socket.State == System.Net.WebSockets.WebSocketState.Open)
    {
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ctx.RequestAborted);
        if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
            break;
    }
});

app.Run();

static Delegate Proxy(
    HttpMethod method,
    string clientName,
    Func<HttpContext, string> pathResolver,
    string serviceName)
    => async (HttpContext context, IHttpClientFactory clients) =>
    {
        var client = clients.CreateClient(clientName);

        var path = pathResolver(context) + context.Request.QueryString;

        using var downstream = new HttpRequestMessage(method, path);

        // Передаём все заголовки, кроме Host/Content-Type/X-User-Id (Content-Type зададим вручную).
        foreach (var header in context.Request.Headers)
        {
            if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                continue;

            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                continue;

            if (header.Key.Equals("X-User-Id", StringComparison.OrdinalIgnoreCase))
                continue;

            downstream.Headers.TryAddWithoutValidation(header.Key, header.Value.AsEnumerable());
        }

        if (context.Request.Headers.TryGetValue("X-User-Id", out var userHeader))
            downstream.Headers.TryAddWithoutValidation("X-User-Id", userHeader.AsEnumerable());

        if (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch)
        {
            downstream.Content = new StreamContent(context.Request.Body);

            if (!string.IsNullOrWhiteSpace(context.Request.ContentType))
                downstream.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(context.Request.ContentType);
        }

        try
        {
            using var response = await client.SendAsync(
                downstream,
                HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted);

            context.Response.StatusCode = (int)response.StatusCode;

            foreach (var header in response.Headers)
                context.Response.Headers[header.Key] = header.Value.ToArray();

            foreach (var header in response.Content.Headers)
                context.Response.Headers[header.Key] = header.Value.ToArray();

            context.Response.Headers.Remove("transfer-encoding");

            await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
        }
        catch (HttpRequestException ex)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(
                new { error = $"Сервис {serviceName} недоступен", detail = ex.Message },
                cancellationToken: context.RequestAborted);
        }
    };
