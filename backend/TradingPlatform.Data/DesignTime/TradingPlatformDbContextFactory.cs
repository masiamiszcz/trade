using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using TradingPlatform.Data.Context;

namespace TradingPlatform.Data.DesignTime;

public sealed class TradingPlatformDbContextFactory : IDesignTimeDbContextFactory<TradingPlatformDbContext>
{
    public TradingPlatformDbContext CreateDbContext(string[] args)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var apiProjectPath = Path.GetFullPath(Path.Combine(currentDirectory, "..", "TradingPlatform.Api"));
        var configurationBasePath = Directory.Exists(apiProjectPath) ? apiProjectPath : currentDirectory;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(configurationBasePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost,1433;Database=TradingDB;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;Encrypt=False";

        var optionsBuilder = new DbContextOptionsBuilder<TradingPlatformDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new TradingPlatformDbContext(optionsBuilder.Options);
    }
}
