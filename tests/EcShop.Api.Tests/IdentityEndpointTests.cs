using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace EcShop.Api.Tests;

public sealed class IdentityEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task RegisterLoginAndAddressEndpointsMaintainAuthenticatedProfile()
    {
        using var client = factory.CreateClient();
        var name = $"user{Guid.NewGuid():N}"[..20];
        using var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new { username = name, email = $"{name}@example.test", password = "correct-horse-battery-staple", agreementAccepted = true });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        using var body = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.RootElement.GetProperty("access_token").GetString());
        using var profile = await client.GetAsync("/api/v1/me"); Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        using var profileUpdate = await client.PatchAsJsonAsync("/api/v1/me", new { email = $"updated-{name}@example.test" }); Assert.Equal(HttpStatusCode.OK, profileUpdate.StatusCode);
        using var comment = await client.PostAsJsonAsync("/api/v1/goods/12/comments", new { content = "已验证评论接口。" });
        Assert.Equal(HttpStatusCode.Created, comment.StatusCode);
        using var comments = await client.GetAsync("/api/v1/goods/12/comments?page=1&page_size=20"); Assert.Equal(HttpStatusCode.OK, comments.StatusCode);
        using var address = await client.PostAsJsonAsync("/api/v1/me/addresses", new { consignee = "测试", countryId = 1, provinceId = 0, cityId = 0, districtId = 0, address = "中山路 1 号", mobile = "13800000000", zipcode = "200000", isDefault = true });
        Assert.Equal(HttpStatusCode.Created, address.StatusCode);
        using var addressBody = JsonDocument.Parse(await address.Content.ReadAsStringAsync());
        var addressId = addressBody.RootElement.GetProperty("id").GetInt64();
        using var addressUpdate = await client.PatchAsJsonAsync($"/api/v1/me/addresses/{addressId}", new { consignee = "新收件人", countryId = 1, provinceId = 0, cityId = 0, districtId = 0, address = "中山路 2 号", mobile = "13800000000", zipcode = "200001", isDefault = true }); Assert.Equal(HttpStatusCode.OK, addressUpdate.StatusCode);
        using var list = await client.GetAsync("/api/v1/me/addresses"); Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var added = await client.PostAsJsonAsync("/api/v1/me/cart", new { goodsId = 12, quantity = 2 }); Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        using var cart = await client.GetAsync("/api/v1/me/cart"); Assert.Equal(HttpStatusCode.OK, cart.StatusCode);
        using var cartBody = JsonDocument.Parse(await cart.Content.ReadAsStringAsync());
        var cartItem = cartBody.RootElement.GetProperty("items")[0];
        using var cartUpdate = await client.PatchAsJsonAsync($"/api/v1/me/cart/{cartItem.GetProperty("id").GetInt64()}", new { quantity = 2, version = cartItem.GetProperty("version").GetInt32() }); Assert.Equal(HttpStatusCode.OK, cartUpdate.StatusCode);
        using var options = await client.GetAsync("/api/v1/checkout/options"); Assert.Equal(HttpStatusCode.OK, options.StatusCode);
        using var quote = await client.PostAsJsonAsync("/api/v1/checkout/quote", new { addressId, shippingId = 1, paymentId = 1 });
        Assert.Equal(HttpStatusCode.OK, quote.StatusCode);
        var idempotencyKey = Guid.NewGuid().ToString("N");
        using var order = await client.PostAsJsonAsync("/api/v1/orders", new { addressId, shippingId = 1, paymentId = 1, idempotencyKey });
        Assert.Equal(HttpStatusCode.Created, order.StatusCode);
        using var orderBody = JsonDocument.Parse(await order.Content.ReadAsStringAsync());
        var orderId = orderBody.RootElement.GetProperty("id").GetInt64();
        using var duplicate = await client.PostAsJsonAsync("/api/v1/orders", new { addressId, shippingId = 1, paymentId = 1, idempotencyKey });
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        using var orders = await client.GetAsync("/api/v1/me/orders"); Assert.True(orders.IsSuccessStatusCode, await orders.Content.ReadAsStringAsync());
        using var detail = await client.GetAsync($"/api/v1/me/orders/{orderId}"); Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        using var cancellation = await client.PostAsync($"/api/v1/me/orders/{orderId}/cancel", null); Assert.Equal(HttpStatusCode.OK, cancellation.StatusCode);
        using var disposableCart = await client.PostAsJsonAsync("/api/v1/me/cart", new { goodsId = 12, quantity = 1 }); Assert.Equal(HttpStatusCode.Created, disposableCart.StatusCode);
        using var disposableCartBody = JsonDocument.Parse(await disposableCart.Content.ReadAsStringAsync());
        using var cartDelete = await client.DeleteAsync($"/api/v1/me/cart/{disposableCartBody.RootElement.GetProperty("id").GetInt64()}"); Assert.Equal(HttpStatusCode.NoContent, cartDelete.StatusCode);
        using var paidCart = await client.PostAsJsonAsync("/api/v1/me/cart", new { goodsId = 12, quantity = 1 }); Assert.Equal(HttpStatusCode.Created, paidCart.StatusCode);
        using var paidOrder = await client.PostAsJsonAsync("/api/v1/orders", new { addressId, shippingId = 1, paymentId = 1, idempotencyKey = Guid.NewGuid().ToString("N") });
        Assert.Equal(HttpStatusCode.Created, paidOrder.StatusCode);
        using var paidOrderBody = JsonDocument.Parse(await paidOrder.Content.ReadAsStringAsync());
        var paidOrderSn = paidOrderBody.RootElement.GetProperty("order_sn").GetString();
        var transactionId = Guid.NewGuid().ToString("N");
        using var callback = await client.PostAsJsonAsync("/api/v1/payments/mock/callback", new { orderSn = paidOrderSn, transactionId }); Assert.Equal(HttpStatusCode.OK, callback.StatusCode);
        using var callbackRetry = await client.PostAsJsonAsync("/api/v1/payments/mock/callback", new { orderSn = paidOrderSn, transactionId }); Assert.Equal(HttpStatusCode.OK, callbackRetry.StatusCode);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var promotion = await client.PostAsJsonAsync("/api/v1/promotions/group-buy", new { name = "测试团购", goodsId = 12, startTime = now - 60, endTime = now + 60 }); Assert.Equal(HttpStatusCode.Created, promotion.StatusCode);
        using var promotions = await client.GetAsync("/api/v1/promotions/group-buy"); Assert.Equal(HttpStatusCode.OK, promotions.StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { username = name, password = "correct-horse-battery-staple" }); Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var loginBody = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.RootElement.GetProperty("access_token").GetString());
        using var logout = await client.PostAsync("/api/v1/auth/logout", null); Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using var loggedOutProfile = await client.GetAsync("/api/v1/me"); Assert.Equal(HttpStatusCode.Unauthorized, loggedOutProfile.StatusCode);
    }
}
