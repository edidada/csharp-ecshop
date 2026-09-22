using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace EcShop.Api.Tests;

public sealed class LegacyContentEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly long[] VoteOptionIds = [1L];

    [Theory]
    [InlineData("/api/v1/activities")]
    [InlineData("/api/v1/announcements")]
    [InlineData("/api/v1/compat/status")]
    [InlineData("/api/v1/compare?goods_ids=12")]
    [InlineData("/api/v1/exchange-goods")]
    [InlineData("/api/v1/feed")]
    [InlineData("/api/v1/goods/12/gallery")]
    [InlineData("/api/v1/goods/12/tags")]
    [InlineData("/api/v1/messages")]
    [InlineData("/api/v1/packages")]
    [InlineData("/api/v1/tags")]
    [InlineData("/api/v1/topics/1")]
    [InlineData("/api/v1/votes/1")]
    [InlineData("/api/v1/wholesale")]
    public async Task LegacyPublicEntryPointReturnsSuccess(string url)
    {
        using var response = await factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CaptchaCanBeVerifiedOnce()
    {
        using var client = factory.CreateClient();
        using var challenge = await client.GetAsync("/api/v1/captcha");
        challenge.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await challenge.Content.ReadAsStringAsync());
        var challengeId = body.RootElement.GetProperty("challenge_id").GetString();
        var operands = body.RootElement.GetProperty("question").GetString()!.Split(" + ").Select(int.Parse).ToArray();
        using var verification = await client.PostAsJsonAsync("/api/v1/captcha/verify", new { challengeId, answer = operands.Sum() });
        Assert.Equal(HttpStatusCode.OK, verification.StatusCode);
        using var replay = await client.PostAsJsonAsync("/api/v1/captcha/verify", new { challengeId, answer = operands.Sum() });
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedUserCanCreateMessageTagAndVote()
    {
        using var client = factory.CreateClient();
        var name = $"legacy{Guid.NewGuid():N}"[..20];
        using var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new { username = name, email = $"{name}@example.test", password = "correct-horse-battery-staple", agreementAccepted = true });
        registration.EnsureSuccessStatusCode();
        using var registrationBody = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registrationBody.RootElement.GetProperty("access_token").GetString());

        using var message = await client.PostAsJsonAsync("/api/v1/messages", new { title = "测试留言", content = "集中测试留言", type = 0, orderId = 0 });
        using var tag = await client.PostAsJsonAsync("/api/v1/goods/12/tags", new { tag = $"tag-{Guid.NewGuid():N}" });
        using var vote = await client.PostAsJsonAsync("/api/v1/votes/1", new { optionIds = VoteOptionIds });

        Assert.Equal(HttpStatusCode.Created, message.StatusCode);
        Assert.Equal(HttpStatusCode.OK, tag.StatusCode);
        Assert.Equal(HttpStatusCode.OK, vote.StatusCode);
    }
}
