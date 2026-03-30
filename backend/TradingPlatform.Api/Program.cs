using TradingPlatform.Core.Extensions;
using TradingPlatform.Data.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "http://127.0.0.1:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddCoreServices();
builder.Services.AddDataServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");

app.MapControllers();

app.MapGet("/", () =>
{
    return Results.Json(new
    {
        message = "Trading Platform API",
        status = "API is working",
        endpoints = new[]
        {
            "/api/market",
            "/api/market/{symbol}"
        },
        timestamp = DateTime.UtcNow
    });
});

app.Run();

public partial class Program { }
