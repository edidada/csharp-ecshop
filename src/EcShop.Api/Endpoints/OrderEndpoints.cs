using EcShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Cryptography;

namespace EcShop.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/v1/orders", CreateAsync);
        routes.MapGet("/api/v1/me/orders", ListAsync);
        routes.MapGet("/api/v1/me/orders/{id:long}", DetailAsync);
        routes.MapPost("/api/v1/me/orders/{id:long}/cancel", CancelAsync);
        return routes;
    }

    private static async Task<IResult> CreateAsync(CreateOrderRequest request, HttpRequest http, EcShopDbContext db, CancellationToken ct)
    {
        var user = await IdentityEndpoints.CurrentUserAsync(http, db, ct);
        if (user is null) return Results.Unauthorized();
        if (request.AddressId <= 0 || request.ShippingId <= 0 || request.PaymentId <= 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey)) return Bad("address_id, shipping_id, payment_id and idempotency_key are required");
        if (request.IdempotencyKey.Length > 128) return Bad("idempotency_key is too long");

        var duplicate = await db.Orders.SingleOrDefaultAsync(x => x.UserId == user.UserId && x.ClientRequestId == request.IdempotencyKey, ct);
        if (duplicate is not null) return Results.Ok(Summary(duplicate));

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var address = await db.Addresses.SingleOrDefaultAsync(x => x.AddressId == request.AddressId && x.UserId == user.UserId, ct);
        var shipping = await db.ShippingMethods.SingleOrDefaultAsync(x => x.ShippingId == request.ShippingId && x.Enabled, ct);
        var payment = await db.PaymentMethods.SingleOrDefaultAsync(x => x.PayId == request.PaymentId && x.Enabled, ct);
        if (address is null || shipping is null || payment is null) return Results.NotFound();

        var cart = await (from item in db.CartItems.AsNoTracking()
                          where item.UserId == user.UserId
                          join goods in db.Goods.AsNoTracking() on item.GoodsId equals goods.GoodsId
                          where goods.IsOnSale && !goods.IsDelete
                          select new { Item = item, Goods = goods }).ToListAsync(ct);
        if (cart.Count == 0) return Bad("cart is empty");
        if (cart.Any(x => x.Item.GoodsNumber > x.Goods.GoodsNumber)) return Results.Conflict(new { code = "out_of_stock" });

        var goodsAmount = cart.Sum(x => x.Item.GoodsNumber * x.Goods.ShopPrice);
        var order = new ShopOrder
        {
            OrderSn = NewOrderSn(), UserId = user.UserId, Consignee = address.Consignee, Address = address.Address,
            ShippingId = shipping.ShippingId, ShippingName = shipping.ShippingName, PayId = payment.PayId, PayName = payment.PayName,
            GoodsAmount = goodsAmount, ShippingFee = shipping.ShippingFee, PayFee = payment.PayFee,
            OrderAmount = goodsAmount + shipping.ShippingFee + payment.PayFee, AddTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), ClientRequestId = request.IdempotencyKey
        };
        db.Orders.Add(order);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            duplicate = await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == user.UserId && x.ClientRequestId == request.IdempotencyKey, ct);
            if (duplicate is not null) return Results.Ok(Summary(duplicate));
            throw;
        }
        foreach (var row in cart)
        {
            var stockUpdated = await db.Goods.Where(x => x.GoodsId == row.Goods.GoodsId && x.GoodsNumber >= row.Item.GoodsNumber).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.GoodsNumber, x => x.GoodsNumber - row.Item.GoodsNumber), ct);
            if (stockUpdated == 0) return Results.Conflict(new { code = "out_of_stock" });
            db.OrderItems.Add(new OrderItem { OrderId = order.OrderId, GoodsId = row.Goods.GoodsId, GoodsName = row.Goods.GoodsName, GoodsSn = row.Goods.GoodsSn, GoodsPrice = row.Goods.ShopPrice, GoodsNumber = row.Item.GoodsNumber });
        }
        await db.CartItems.Where(x => x.UserId == user.UserId).ExecuteDeleteAsync(ct);
        db.OrderActions.Add(new OrderAction { OrderId = order.OrderId, UserId = user.UserId, ActionNote = "created", LogTime = order.AddTime });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.Created($"/api/v1/me/orders/{order.OrderId}", Summary(order));
    }

    private static async Task<IResult> ListAsync(HttpRequest http, EcShopDbContext db, CancellationToken ct)
    {
        var user = await IdentityEndpoints.CurrentUserAsync(http, db, ct); if (user is null) return Results.Unauthorized();
        var page = ParsePage(http.Query["page"]); var pageSize = ParsePageSize(http.Query["page_size"]);
        var query = db.Orders.Where(x => x.UserId == user.UserId).OrderByDescending(x => x.OrderId);
        var total = await query.CountAsync(ct); var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Results.Ok(new { page, page_size = pageSize, total, items = items.Select(Summary) });
    }

    private static async Task<IResult> DetailAsync(long id, HttpRequest http, EcShopDbContext db, CancellationToken ct)
    {
        var user = await IdentityEndpoints.CurrentUserAsync(http, db, ct); if (user is null) return Results.Unauthorized();
        var order = await db.Orders.SingleOrDefaultAsync(x => x.OrderId == id && x.UserId == user.UserId, ct); if (order is null) return Results.NotFound();
        var items = await db.OrderItems.Where(x => x.OrderId == id).ToListAsync(ct);
        return Results.Ok(new { order = Summary(order), items = items.Select(x => new { goods_id = x.GoodsId, name = x.GoodsName, goods_sn = x.GoodsSn, price = Money(x.GoodsPrice), quantity = x.GoodsNumber }) });
    }

    private static async Task<IResult> CancelAsync(long id, HttpRequest http, EcShopDbContext db, CancellationToken ct)
    {
        var user = await IdentityEndpoints.CurrentUserAsync(http, db, ct); if (user is null) return Results.Unauthorized();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.OrderId == id && x.UserId == user.UserId, ct); if (order is null) return Results.NotFound();
        if (order.OrderStatus != 0 || order.PayStatus != 0) return Results.Conflict(new { code = "order_not_cancellable" });
        var cancelled = await db.Orders.Where(x => x.OrderId == id && x.UserId == user.UserId && x.OrderStatus == 0 && x.PayStatus == 0).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.OrderStatus, 2), ct);
        if (cancelled == 0) return Results.Conflict(new { code = "order_not_cancellable" });
        var items = await db.OrderItems.AsNoTracking().Where(x => x.OrderId == id).ToListAsync(ct);
        foreach (var item in items) await db.Goods.Where(x => x.GoodsId == item.GoodsId).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.GoodsNumber, x => x.GoodsNumber + item.GoodsNumber), ct);
        order.OrderStatus = 2;
        db.OrderActions.Add(new OrderAction { OrderId = id, UserId = user.UserId, ActionNote = "cancelled_by_user", LogTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() });
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Results.Ok(Summary(order));
    }

    internal static object Summary(ShopOrder order) => new { id = order.OrderId, order_sn = order.OrderSn, order_status = order.OrderStatus, pay_status = order.PayStatus, shipping_status = order.ShippingStatus, goods_amount = Money(order.GoodsAmount), shipping_fee = Money(order.ShippingFee), payment_fee = Money(order.PayFee), order_amount = Money(order.OrderAmount), created_at = order.AddTime };
    private static string NewOrderSn() => $"CS{DateTimeOffset.UtcNow:yyyyMMddHHmmss}{RandomNumberGenerator.GetInt32(100000, 999999)}";
    private static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    private static int ParsePage(string? value) => int.TryParse(value, out var page) ? Math.Max(page, 1) : 1;
    private static int ParsePageSize(string? value) => int.TryParse(value, out var pageSize) ? Math.Clamp(pageSize, 1, 100) : 20;
    private static IResult Bad(string detail) => Results.Problem(statusCode: 400, title: "validation_error", detail: detail);
}

public sealed record CreateOrderRequest(long AddressId, long ShippingId, long PaymentId, string IdempotencyKey);
