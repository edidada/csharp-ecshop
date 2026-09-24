using EcShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace EcShop.Api.Endpoints;

public static class CartEndpoints
{
    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/v1/me/cart", GetAsync); routes.MapPost("/api/v1/me/cart", AddAsync); routes.MapPatch("/api/v1/me/cart/{id:long}", UpdateAsync); routes.MapDelete("/api/v1/me/cart/{id:long}", DeleteAsync); return routes;
    }
    private static async Task<IResult> GetAsync(HttpRequest http, EcShopDbContext db, CancellationToken ct)
    {
        var user = await IdentityEndpoints.CurrentUserAsync(http, db, ct); if (user is null) return Results.Unauthorized();
        var rows = await (from cart in db.CartItems where cart.UserId == user.UserId join goods in db.Goods on cart.GoodsId equals goods.GoodsId where goods.IsOnSale && !goods.IsDelete select new { cart, goods }).ToListAsync(ct);
        return Results.Ok(new { items = rows.Select(x => Item(x.cart, x.goods)), total_quantity = rows.Sum(x => x.cart.GoodsNumber) });
    }
    private static async Task<IResult> AddAsync(AddCartRequest request, HttpRequest http, EcShopDbContext db, CancellationToken ct)
    {
        var user = await IdentityEndpoints.CurrentUserAsync(http, db, ct); if (user is null) return Results.Unauthorized(); if (request.GoodsId <= 0 || request.Quantity is < 1 or > 999) return Bad("goods_id and quantity are invalid");
        var goods = await db.Goods.SingleOrDefaultAsync(x => x.GoodsId == request.GoodsId && x.IsOnSale && !x.IsDelete, ct); if (goods is null) return Results.NotFound();
        var item = await db.CartItems.SingleOrDefaultAsync(x => x.UserId == user.UserId && x.GoodsId == request.GoodsId, ct);
        if (item is null) { item = new CartItem { UserId = user.UserId, GoodsId = request.GoodsId, GoodsNumber = request.Quantity }; db.CartItems.Add(item); } else { item.GoodsNumber = checked(item.GoodsNumber + request.Quantity); item.Version++; }
        if (item.GoodsNumber > goods.GoodsNumber) return Results.Conflict(new { code = "out_of_stock" }); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/me/cart/{item.RecId}", Item(item, goods));
    }
    private static async Task<IResult> UpdateAsync(long id, UpdateCartRequest request, HttpRequest http, EcShopDbContext db, CancellationToken ct)
    {
        var user = await IdentityEndpoints.CurrentUserAsync(http, db, ct); if (user is null) return Results.Unauthorized(); if (request.Quantity is < 1 or > 999) return Bad("quantity is invalid");
        var item = await db.CartItems.AsNoTracking().SingleOrDefaultAsync(x => x.RecId == id && x.UserId == user.UserId, ct); if (item is null) return Results.NotFound();
        var goods = await db.Goods.AsNoTracking().SingleOrDefaultAsync(x => x.GoodsId == item.GoodsId && x.IsOnSale && !x.IsDelete, ct); if (goods is null) return Results.NotFound(); if (request.Quantity > goods.GoodsNumber) return Results.Conflict(new { code = "out_of_stock" });
        var updated = await db.CartItems.Where(x => x.RecId == id && x.UserId == user.UserId && x.Version == request.Version).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.GoodsNumber, request.Quantity).SetProperty(x => x.Version, x => x.Version + 1), ct);
        if (updated == 0) return Results.Conflict(new { code = "version_conflict" }); item.GoodsNumber = request.Quantity; item.Version = request.Version + 1; return Results.Ok(Item(item, goods));
    }
    private static async Task<IResult> DeleteAsync(long id, HttpRequest http, EcShopDbContext db, CancellationToken ct) { var user = await IdentityEndpoints.CurrentUserAsync(http, db, ct); if (user is null) return Results.Unauthorized(); var item = await db.CartItems.SingleOrDefaultAsync(x => x.RecId == id && x.UserId == user.UserId, ct); if (item is null) return Results.NotFound(); db.CartItems.Remove(item); await db.SaveChangesAsync(ct); return Results.NoContent(); }
    private static object Item(CartItem item, Goods goods) => new { id = item.RecId, goods_id = item.GoodsId, name = goods.GoodsName, price = goods.ShopPrice.ToString("0.00", CultureInfo.InvariantCulture), quantity = item.GoodsNumber, version = item.Version };
    private static IResult Bad(string d) => Results.Problem(statusCode: 400, title: "validation_error", detail: d);
}
public sealed record AddCartRequest(long GoodsId, int Quantity); public sealed record UpdateCartRequest(int Quantity, int Version);
