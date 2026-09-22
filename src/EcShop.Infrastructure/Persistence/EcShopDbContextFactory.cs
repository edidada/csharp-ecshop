using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EcShop.Infrastructure.Persistence;

/// <summary>Enables <c>dotnet ef</c> without requiring a running service.</summary>
public sealed class EcShopDbContextFactory : IDesignTimeDbContextFactory<EcShopDbContext>
{
    public EcShopDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<EcShopDbContext>()
            .UseSqlite("Data Source=ecshop.design.db")
            .Options;
        return new EcShopDbContext(options);
    }
}
