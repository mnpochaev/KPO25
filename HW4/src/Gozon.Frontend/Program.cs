var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/config.js", (IConfiguration cfg) =>
{
    var apiBase = cfg["Backend:ApiBase"] ?? "http://localhost:5241";
    var wsBase = cfg["Backend:WsBase"] ?? "http://localhost:5241/ws";
    return Results.Content($"window.APP_CONFIG = {{ apiBase: '{apiBase}', wsBase: '{wsBase}' }};", "application/javascript");
});

app.Run();
