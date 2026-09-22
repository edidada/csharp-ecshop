using Microsoft.Extensions.DependencyInjection;

namespace EcShop.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register cross-endpoint workflows here when a vertical slice needs reusable application logic.
        return services;
    }
}
