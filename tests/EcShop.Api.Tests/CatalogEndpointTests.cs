using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace EcShop.Api.Tests;

public sealed class CatalogEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly long[] StandardProductOptions = [1001, 1005];

    [Fact]
    public async Task HomeReturnsPublicCatalogData()
    {
        using var response = await factory.CreateClient().GetAsync("/api/v1/home");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("goods").GetArrayLength() > 0);
        Assert.True(body.RootElement.GetProperty("categories").GetArrayLength() > 0);
    }

    [Fact]
    public async Task CategoryAndSearchReturnPaginatedVisibleGoods()
    {
        using var client = factory.CreateClient();
        using var category = await client.GetAsync("/api/v1/categories/1/goods?page=1&page_size=20");
        using var search = await client.GetAsync("/api/v1/goods?q=%E5%85%A5%E9%97%A8&sort=price_asc");
        Assert.Equal(HttpStatusCode.OK, category.StatusCode);
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);
        using var categoryBody = JsonDocument.Parse(await category.Content.ReadAsStringAsync());
        Assert.Equal(1, categoryBody.RootElement.GetProperty("total").GetInt32());
        using var searchBody = JsonDocument.Parse(await search.Content.ReadAsStringAsync());
        Assert.Equal(12, searchBody.RootElement.GetProperty("items")[0].GetProperty("id").GetInt64());
    }

    [Fact]
    public async Task GoodsDetailAndPriceQuoteUseCurrentAttributes()
    {
        using var client = factory.CreateClient();
        using var detail = await client.GetAsync("/api/v1/goods/12");
        using var quote = await client.PostAsJsonAsync("/api/v1/goods/12/price-quote", new { quantity = 2, productId = 81, attributeIds = StandardProductOptions });
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Equal(HttpStatusCode.OK, quote.StatusCode);
        using var detailBody = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.Equal(2, detailBody.RootElement.GetProperty("attributes").GetArrayLength());
        using var quoteBody = JsonDocument.Parse(await quote.Content.ReadAsStringAsync());
        Assert.Equal("109.80", quoteBody.RootElement.GetProperty("total").GetString());
    }

    [Fact]
    public async Task BrandArticleAndRegionEndpointsReturnMappedResources()
    {
        using var client = factory.CreateClient();
        using var brands = await client.GetAsync("/api/v1/brands");
        using var brandGoods = await client.GetAsync("/api/v1/brands/1/goods");
        using var article = await client.GetAsync("/api/v1/articles/1");
        using var articles = await client.GetAsync("/api/v1/article-categories/1/articles");
        using var regions = await client.GetAsync("/api/v1/regions?parent=0&type=1");
        Assert.Equal(HttpStatusCode.OK, brands.StatusCode);
        Assert.Equal(HttpStatusCode.OK, brandGoods.StatusCode);
        Assert.Equal(HttpStatusCode.OK, article.StatusCode);
        Assert.Equal(HttpStatusCode.OK, articles.StatusCode);
        Assert.Equal(HttpStatusCode.OK, regions.StatusCode);
    }

    [Fact]
    public async Task UnavailableGoodsAreNotPubliclyVisible()
    {
        using var response = await factory.CreateClient().GetAsync("/api/v1/goods/13");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
