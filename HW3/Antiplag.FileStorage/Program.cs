using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Регистрируем MVC-контроллеры и Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Antiplag.FileStorage API",
        Version = "v1"
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Antiplag.FileStorage v1");
    });
}

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.Run();