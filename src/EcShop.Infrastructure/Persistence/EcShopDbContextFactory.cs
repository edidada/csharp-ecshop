using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using EcShop.Infrastructure;

namespace EcShop.Infrastructure.Persistence;

/// <summary>Enables <c>dotnet ef</c> without requiring a running service.</summary>
public sealed class EcShopDbContextFactory : IDesignTimeDbContextFactory<EcShopDbContext>
{
    public EcShopDbContext CreateDbContext(string[] args)
    {
        var database = new DatabaseOptions
        {
            Provider = Environment.GetEnvironmentVariable("Database__Provider") ?? "Sqlite",
            ConnectionString = Environment.GetEnvironmentVariable("Database__ConnectionString") ?? "Data Source=ecshop.design.db"
        };
        var builder = new DbContextOptionsBuilder<EcShopDbContext>();
        DependencyInjection.ConfigureDatabase(builder, database);
        return new EcShopDbContext(builder.Options);
    }
}
