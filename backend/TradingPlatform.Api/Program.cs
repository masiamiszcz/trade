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

builder.Services.AddDataServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");

app.MapControllers();

// Run database migrations in background
_ = Task.Run(async () =>
{
    try
    {
        await app.Services.ApplyDatabaseMigrationsAsync();
    }
    catch (Exception ex)
    {
        // Log error but don't crash the app
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to apply database migrations");
    }
});

app.Run();

public partial class Program { }
