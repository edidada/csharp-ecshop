using EcShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EcShop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var database = new DatabaseOptions
        {
            Provider = configuration[$"{DatabaseOptions.SectionName}:Provider"] ?? "Sqlite",
            ConnectionString = configuration[$"{DatabaseOptions.SectionName}:ConnectionString"] ?? string.Empty
        };
        if (string.IsNullOrWhiteSpace(database.ConnectionString))
        {
            throw new InvalidOperationException("Database:ConnectionString must not be empty.");
        }

        services.AddDbContext<EcShopDbContext>(options => ConfigureDatabase(options, database));
        return services;
    }

    internal static void ConfigureDatabase(DbContextOptionsBuilder options, DatabaseOptions database)
    {
        switch (database.Provider.Trim().ToLowerInvariant())
        {
            case "sqlite": options.UseSqlite(database.ConnectionString); break;
            case "postgresql" or "postgres": options.UseNpgsql(database.ConnectionString); break;
            case "mysql": options.UseMySql(database.ConnectionString, new MySqlServerVersion(new Version(8, 0, 0))); break;
            default: throw new InvalidOperationException($"Unsupported database provider '{database.Provider}'. Use Sqlite, PostgreSql, or MySql.");
        }
    }
}
