using System.Net;
using Xunit;

namespace EcShop.Api.Tests;

public sealed class HealthEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task LivenessEndpointReturnsSuccess()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/healthz");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ReadinessEndpointReturnsSuccessAfterSqliteDatabaseIsProvisioned()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/readyz");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
