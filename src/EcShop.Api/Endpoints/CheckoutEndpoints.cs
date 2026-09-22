using EcShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace EcShop.Api.Endpoints;

public static class CheckoutEndpoints
{
    public static IEndpointRouteBuilder MapCheckoutEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api/v1/checkout"); api.MapGet("/options", OptionsAsync); api.MapPost("/quote", QuoteAsync); return routes;
    }
    private static async Task<IResult> OptionsAsync(HttpRequest http, EcShopDbContext db, CancellationToken ct)
    {
        if (await IdentityEndpoints.CurrentUserAsync(http, db, ct) is null) return Results.Unauthorized();
        var shipping = await db.ShippingMethods.Where(x => x.Enabled).ToListAsync(ct); var payments = await db.PaymentMethods.Where(x => x.Enabled).ToListAsync(ct);
        return Results.Ok(new { shipping = shipping.Select(x => new { id = x.ShippingId, name = x.ShippingName, fee = Money(x.ShippingFee) }), payments = payments.Select(x => new { id = x.PayId, name = x.PayName, fee = Money(x.PayFee) }) });
    }
    private static async Task<IResult> QuoteAsync(CheckoutQuoteRequest r, HttpRequest http, EcShopDbContext db, CancellationToken ct)
    {
        var user = await IdentityEndpoints.CurrentUserAsync(http, db, ct); if (user is null) return Results.Unauthorized();
        var address = await db.Addresses.SingleOrDefaultAsync(x => x.AddressId == r.AddressId && x.UserId == user.UserId, ct); var shipping = await db.ShippingMethods.SingleOrDefaultAsync(x => x.ShippingId == r.ShippingId && x.Enabled, ct); var payment = await db.PaymentMethods.SingleOrDefaultAsync(x => x.PayId == r.PaymentId && x.Enabled, ct);
        if (address is null || shipping is null || payment is null) return Results.NotFound();
        var items = await (from cart in db.CartItems where cart.UserId == user.UserId join goods in db.Goods on cart.GoodsId equals goods.GoodsId where goods.IsOnSale && !goods.IsDelete select new { cart, goods }).ToListAsync(ct); if (items.Count == 0) return Results.Problem(statusCode: 400, title: "validation_error", detail: "cart is empty");
        var amount = items.Sum(x => x.cart.GoodsNumber * x.goods.ShopPrice); return Results.Ok(new { address_id = address.AddressId, shipping_id = shipping.ShippingId, payment_id = payment.PayId, items = items.Select(x => new { goods_id = x.goods.GoodsId, quantity = x.cart.GoodsNumber, price = Money(x.goods.ShopPrice) }), goods_amount = Money(amount), shipping_fee = Money(shipping.ShippingFee), payment_fee = Money(payment.PayFee), order_amount = Money(amount + shipping.ShippingFee + payment.PayFee) });
    }
    private static string Money(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);
}
public sealed record CheckoutQuoteRequest(long AddressId, long ShippingId, long PaymentId);
