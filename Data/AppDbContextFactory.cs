using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FiveThreeOneTracker.Data;

/// <summary>
/// Design-time factory used exclusively by EF Core tooling (dotnet ef migrations).
/// Always targets PostgreSQL so generated migrations use Npgsql column types.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<AppDbContextFactory>(optional: true)
            .Build();

        // Read DATABASE_URL from the environment if present (same as runtime),
        // otherwise use the local developer secret for scaffolding.
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Set DATABASE_URL or the ConnectionStrings:Postgres user secret before using EF tooling.");

        var connectionString = ConvertDatabaseUrl(databaseUrl);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }

    internal static string ConvertDatabaseUrl(string databaseUrl)
    {
        // Digital Ocean supplies DATABASE_URL as postgresql://user:pass@host:port/db
        if (databaseUrl.StartsWith("postgresql://") || databaseUrl.StartsWith("postgres://"))
        {
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');
            return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
        }

        // Already in Npgsql connection string format
        return databaseUrl;
    }
}
