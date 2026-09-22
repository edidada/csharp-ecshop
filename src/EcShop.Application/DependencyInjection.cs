using Microsoft.Extensions.DependencyInjection;

namespace EcShop.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // URL use cases are registered here as each PHP endpoint is ported.
        return services;
    }
}
