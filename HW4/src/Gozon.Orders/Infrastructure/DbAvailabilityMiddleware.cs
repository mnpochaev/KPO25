using Microsoft.EntityFrameworkCore;

namespace Gozon.Orders.Infrastructure;

public sealed class DbAvailabilityMiddleware<TDbContext> where TDbContext : DbContext
{
    private readonly RequestDelegate _next;

    public DbAvailabilityMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx, IServiceProvider sp)
    {
        var path = ctx.Request.Path.Value ?? "";

        // Пропускаем всегда: health/ready/swagger (чтобы сервис был доступен даже без БД)
        if (path.StartsWith("/health") || path.StartsWith("/ready") || path.StartsWith("/swagger"))
        {
            await _next(ctx);
            return;
        }

        try
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TDbContext>();

            var canConnect = await db.Database.CanConnectAsync(ctx.RequestAborted);
            if (!canConnect)
            {
                ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await ctx.Response.WriteAsJsonAsync(new { error = "База данных недоступна" });
                return;
            }
        }
        catch
        {
            ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await ctx.Response.WriteAsJsonAsync(new { error = "База данных недоступна" });
            return;
        }

        await _next(ctx);
    }
}
