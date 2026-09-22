using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcShop.Api.Tests;

public sealed class RouteInventoryTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly string[] ExpectedRoutes =
    [
        "GET /healthz",
        "GET /readyz",
        "GET /api/v1/home",
        "GET /api/v1/categories/{id:long}/goods",
        "GET /api/v1/goods/{id:long}",
        "POST /api/v1/goods/{id:long}/price-quote",
        "GET /api/v1/goods",
        "GET /api/v1/brands",
        "GET /api/v1/brands/{id:long}/goods",
        "GET /api/v1/articles/{id:long}",
        "GET /api/v1/article-categories/{id:long}/articles",
        "GET /api/v1/regions",
        "POST /api/v1/auth/register",
        "POST /api/v1/auth/login",
        "POST /api/v1/auth/logout",
        "GET /api/v1/me",
        "PATCH /api/v1/me",
        "GET /api/v1/me/addresses",
        "POST /api/v1/me/addresses",
        "PATCH /api/v1/me/addresses/{id:long}",
        "GET /api/v1/me/cart",
        "POST /api/v1/me/cart",
        "PATCH /api/v1/me/cart/{id:long}",
        "DELETE /api/v1/me/cart/{id:long}",
        "GET /api/v1/checkout/options",
        "POST /api/v1/checkout/quote",
        "POST /api/v1/orders",
        "GET /api/v1/me/orders",
        "GET /api/v1/me/orders/{id:long}",
        "POST /api/v1/me/orders/{id:long}/cancel",
        "GET /api/v1/goods/{id:long}/comments",
        "POST /api/v1/goods/{id:long}/comments",
        "POST /api/v1/payments/{provider}/callback",
        "GET /api/v1/promotions/{kind}",
        "POST /api/v1/promotions/{kind}",
        "GET /api/v1/activities",
        "GET /api/v1/announcements",
        "GET /api/v1/compat/status",
        "GET /api/v1/captcha",
        "POST /api/v1/captcha/verify",
        "GET /api/v1/compare",
        "GET /api/v1/exchange-goods",
        "GET /api/v1/feed",
        "GET /api/v1/goods/{id:long}/gallery",
        "GET /api/v1/goods/{id:long}/tags",
        "POST /api/v1/goods/{id:long}/tags",
        "GET /api/v1/messages",
        "POST /api/v1/messages",
        "GET /api/v1/packages",
        "GET /api/v1/tags",
        "GET /api/v1/topics/{id:long}",
        "GET /api/v1/votes/{id:long}",
        "POST /api/v1/votes/{id:long}",
        "GET /api/v1/wholesale"
    ];

    [Fact]
    public void RegisteredRoutesMatchTheDocumentedInventory()
    {
        _ = factory.CreateClient();
        var routes = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .SelectMany(endpoint => endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods
                .Select(method => $"{method} /{endpoint.RoutePattern.RawText?.TrimStart('/')}") ?? [])
            .OrderBy(route => route, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedRoutes.OrderBy(route => route, StringComparer.Ordinal), routes);
    }
}
