using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace EcShop.Api.Tests;

public sealed class NegativeEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly long[] VoteOptionIds = [1L];

    [Theory]
    [InlineData("/api/v1/me")]
    [InlineData("/api/v1/me/addresses")]
    [InlineData("/api/v1/me/cart")]
    [InlineData("/api/v1/checkout/options")]
    [InlineData("/api/v1/me/orders")]
    public async Task ProtectedReadsRejectAnonymousRequests(string url)
    {
        using var response = await factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CartRejectsAStaleVersion()
    {
        using var client = await CreateAuthenticatedClientAsync();
        using var added = await client.PostAsJsonAsync("/api/v1/me/cart", new { goodsId = 12, quantity = 1 });
        added.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await added.Content.ReadAsStringAsync());
        var id = body.RootElement.GetProperty("id").GetInt64();
        var version = body.RootElement.GetProperty("version").GetInt32();

        using var first = await client.PatchAsJsonAsync($"/api/v1/me/cart/{id}", new { quantity = 2, version });
        using var stale = await client.PatchAsJsonAsync($"/api/v1/me/cart/{id}", new { quantity = 3, version });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
    }

    [Fact]
    public async Task VoteRejectsASecondSubmissionByTheSameUser()
    {
        using var client = await CreateAuthenticatedClientAsync();
        using var first = await client.PostAsJsonAsync("/api/v1/votes/1", new { optionIds = VoteOptionIds });
        using var duplicate = await client.PostAsJsonAsync("/api/v1/votes/1", new { optionIds = VoteOptionIds });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/compare", HttpStatusCode.BadRequest)]
    [InlineData("/api/v1/compare?goods_ids=13", HttpStatusCode.NotFound)]
    public async Task CompareRejectsInvalidOrHiddenGoods(string url, HttpStatusCode expected)
    {
        using var response = await factory.CreateClient().GetAsync(url);
        Assert.Equal(expected, response.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = factory.CreateClient();
        var name = $"negative{Guid.NewGuid():N}"[..24];
        using var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new { username = name, email = $"{name}@example.test", password = "correct-horse-battery-staple", agreementAccepted = true });
        registration.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.RootElement.GetProperty("access_token").GetString());
        return client;
    }
}
