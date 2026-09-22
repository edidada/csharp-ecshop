using EcShop.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace EcShop.Api.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api/v1");
        api.MapGet("/home", HomeAsync);
        api.MapGet("/categories/{id:long}/goods", CategoryGoodsAsync);
        api.MapGet("/goods/{id:long}", GoodsAsync);
        api.MapPost("/goods/{id:long}/price-quote", PriceQuoteAsync);
        api.MapGet("/goods", GoodsListAsync);
        api.MapGet("/brands", BrandsAsync);
        api.MapGet("/brands/{id:long}/goods", BrandGoodsAsync);
        api.MapGet("/articles/{id:long}", ArticleAsync);
        api.MapGet("/article-categories/{id:long}/articles", CategoryArticlesAsync);
        api.MapGet("/regions", RegionsAsync);
        return routes;
    }

    private static async Task<IResult> HomeAsync(EcShopDbContext db, CancellationToken cancellationToken)
    {
        var goods = await VisibleGoods(db).OrderByDescending(item => item.IsBest).ThenByDescending(item => item.AddTime).Take(12).ToListAsync(cancellationToken);
        var categories = await db.Categories.AsNoTracking().Where(item => item.IsShow && item.ParentId == 0).OrderBy(item => item.CatId).ToListAsync(cancellationToken);
        var articles = await db.Articles.AsNoTracking().Where(item => item.IsOpen).OrderByDescending(item => item.ArticleId).Take(8).ToListAsync(cancellationToken);
        return Results.Ok(new { goods = goods.Select(GoodsSummary), categories = categories.Select(item => new { id = item.CatId, name = item.CatName }), articles = articles.Select(item => new { id = item.ArticleId, title = item.Title, summary = item.ArticleDesc }) });
    }

    private static async Task<IResult> CategoryGoodsAsync(long id, int? page, [FromQuery(Name = "page_size")] int? pageSize, EcShopDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.Categories.AnyAsync(item => item.CatId == id && item.IsShow, cancellationToken)) return NotFound("category not found");
        return Results.Ok(await PageAsync(VisibleGoods(db).Where(item => item.CatId == id).OrderBy(item => item.GoodsId), page, pageSize, cancellationToken));
    }

    private static async Task<IResult> GoodsAsync(long id, EcShopDbContext db, CancellationToken cancellationToken)
    {
        var goods = await VisibleGoods(db).SingleOrDefaultAsync(item => item.GoodsId == id, cancellationToken);
        if (goods is null) return NotFound("goods not found");
        var attributes = await db.GoodsAttributes.AsNoTracking().Where(item => item.GoodsId == id).OrderBy(item => item.GoodsAttrId).ToListAsync(cancellationToken);
        var products = await db.Products.AsNoTracking().Where(item => item.GoodsId == id).OrderBy(item => item.ProductId).ToListAsync(cancellationToken);
        return Results.Ok(new { id = goods.GoodsId, name = goods.GoodsName, goods_sn = goods.GoodsSn, category_id = goods.CatId, brand_id = goods.BrandId, price = Money(goods.ShopPrice), market_price = Money(goods.MarketPrice), brief = goods.GoodsBrief, description = goods.GoodsDesc, image = goods.GoodsImg, thumbnail = goods.GoodsThumb, stock_available = goods.GoodsNumber, attributes = attributes.Select(item => new { id = item.GoodsAttrId, attribute_id = item.AttrId, value = item.AttrValue, price_delta = Money(item.AttrPrice) }), products = products.Select(item => new { id = item.ProductId, stock_available = item.ProductNumber }) });
    }

    private static async Task<IResult> PriceQuoteAsync(long id, PriceQuoteRequest request, EcShopDbContext db, CancellationToken cancellationToken)
    {
        if (request.Quantity is < 1 or > 999) return Validation("quantity must be between 1 and 999");
        var goods = await VisibleGoods(db).SingleOrDefaultAsync(item => item.GoodsId == id, cancellationToken);
        if (goods is null) return NotFound("goods not found");
        var attributeIds = request.AttributeIds?.Distinct().ToArray() ?? [];
        var attributes = await db.GoodsAttributes.AsNoTracking().Where(item => item.GoodsId == id && attributeIds.Contains(item.GoodsAttrId)).ToListAsync(cancellationToken);
        if (attributes.Count != attributeIds.Length) return Validation("attribute_ids contains an attribute that does not belong to this goods");
        var stock = goods.GoodsNumber;
        if (request.ProductId is long productId)
        {
            var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(item => item.ProductId == productId && item.GoodsId == id, cancellationToken);
            if (product is null) return Validation("product_id does not belong to this goods");
            stock = product.ProductNumber;
        }
        var unitPrice = goods.ShopPrice + attributes.Sum(item => item.AttrPrice);
        return Results.Ok(new { goods_id = id, quantity = request.Quantity, unit_price = Money(unitPrice), total = Money(unitPrice * request.Quantity), currency = "CNY", stock_available = stock });
    }

    private static async Task<IResult> GoodsListAsync([FromQuery(Name = "category_id")] long? categoryId, [FromQuery(Name = "brand_id")] long? brandId, string? q, string? sort, int? page, [FromQuery(Name = "page_size")] int? pageSize, EcShopDbContext db, CancellationToken cancellationToken)
    {
        var query = VisibleGoods(db);
        if (categoryId is long category) query = query.Where(item => item.CatId == category);
        if (brandId is long brand) query = query.Where(item => item.BrandId == brand);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(item => item.GoodsName.Contains(term) || item.Keywords.Contains(term));
        }
        query = sort switch
        {
            "price_asc" => query.OrderBy(item => (double)item.ShopPrice),
            "price_desc" => query.OrderByDescending(item => (double)item.ShopPrice),
            "sales" => query.OrderByDescending(item => item.GoodsId),
            null or "" or "newest" => query.OrderByDescending(item => item.AddTime).ThenByDescending(item => item.GoodsId),
            _ => null!
        };
        if (query is null) return Validation("sort must be price_asc, price_desc, newest, or sales");
        return Results.Ok(await PageAsync(query, page, pageSize, cancellationToken));
    }

    private static async Task<IResult> BrandsAsync(EcShopDbContext db, CancellationToken cancellationToken)
    {
        var brands = await db.Brands.AsNoTracking().Where(item => item.IsShow).OrderBy(item => item.BrandId).ToListAsync(cancellationToken);
        return Results.Ok(brands.Select(item => new { id = item.BrandId, name = item.BrandName, logo = item.BrandLogo, description = item.BrandDesc, site_url = item.SiteUrl }));
    }

    private static async Task<IResult> BrandGoodsAsync(long id, int? page, [FromQuery(Name = "page_size")] int? pageSize, EcShopDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.Brands.AnyAsync(item => item.BrandId == id && item.IsShow, cancellationToken)) return NotFound("brand not found");
        return Results.Ok(await PageAsync(VisibleGoods(db).Where(item => item.BrandId == id).OrderBy(item => item.GoodsId), page, pageSize, cancellationToken));
    }

    private static async Task<IResult> ArticleAsync(long id, EcShopDbContext db, CancellationToken cancellationToken)
    {
        var article = await db.Articles.AsNoTracking().SingleOrDefaultAsync(item => item.ArticleId == id && item.IsOpen, cancellationToken);
        return article is null ? NotFound("article not found") : Results.Ok(new { id = article.ArticleId, category_id = article.CatId, title = article.Title, author = article.Author, summary = article.ArticleDesc, content = article.Content });
    }

    private static async Task<IResult> CategoryArticlesAsync(long id, int? page, [FromQuery(Name = "page_size")] int? pageSize, EcShopDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.ArticleCategories.AnyAsync(item => item.CatId == id && item.IsShow, cancellationToken)) return NotFound("article category not found");
        var currentPage = NormalizePage(page); var size = NormalizePageSize(pageSize);
        var query = db.Articles.AsNoTracking().Where(item => item.CatId == id && item.IsOpen).OrderByDescending(item => item.ArticleId);
        var total = await query.CountAsync(cancellationToken);
        var articles = await query.Skip((currentPage - 1) * size).Take(size).ToListAsync(cancellationToken);
        return Results.Ok(new { page = currentPage, page_size = size, total, items = articles.Select(item => new { id = item.ArticleId, title = item.Title, summary = item.ArticleDesc }) });
    }

    private static async Task<IResult> RegionsAsync(long? parent, int? type, EcShopDbContext db, CancellationToken cancellationToken)
    {
        var query = db.Regions.AsNoTracking().AsQueryable();
        if (parent is long parentId) query = query.Where(item => item.ParentId == parentId);
        if (type is int regionType) query = query.Where(item => item.RegionType == regionType);
        var regions = await query.OrderBy(item => item.RegionId).ToListAsync(cancellationToken);
        return Results.Ok(regions.Select(item => new { id = item.RegionId, parent_id = item.ParentId, name = item.RegionName, type = item.RegionType }));
    }

    private static IQueryable<Goods> VisibleGoods(EcShopDbContext db) => db.Goods.AsNoTracking().Where(item => item.IsOnSale && !item.IsDelete);
    private static object GoodsSummary(Goods item) => new { id = item.GoodsId, name = item.GoodsName, category_id = item.CatId, brand_id = item.BrandId, price = Money(item.ShopPrice), thumbnail = item.GoodsThumb, stock_available = item.GoodsNumber };
    private static async Task<object> PageAsync(IQueryable<Goods> query, int? page, int? pageSize, CancellationToken cancellationToken) { var currentPage = NormalizePage(page); var size = NormalizePageSize(pageSize); var total = await query.CountAsync(cancellationToken); var items = await query.Skip((currentPage - 1) * size).Take(size).ToListAsync(cancellationToken); return new { page = currentPage, page_size = size, total, items = items.Select(GoodsSummary) }; }
    private static int NormalizePage(int? page) => page is > 0 ? page.Value : 1;
    private static int NormalizePageSize(int? size) => size is > 0 and <= 100 ? size.Value : 20;
    private static string Money(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);
    private static IResult NotFound(string message) => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "not_found", detail: message);
    private static IResult Validation(string message) => Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "validation_error", detail: message);
}

public sealed record PriceQuoteRequest(int Quantity, long? ProductId, long[]? AttributeIds);
