using EcShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EcShop.Api.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/v1/payments/{provider}/callback", CallbackAsync);
        return routes;
    }

    private static async Task<IResult> CallbackAsync(string provider, PaymentCallbackRequest request, EcShopDbContext db, CancellationToken ct)
    {
        // The mock adapter is deliberately the only built-in callback. Real providers belong in an adapter that validates its signature before this handler is invoked.
        if (!string.Equals(provider, "mock", StringComparison.Ordinal)) return Results.NotFound();
        if (string.IsNullOrWhiteSpace(request.OrderSn) || string.IsNullOrWhiteSpace(request.TransactionId)) return Results.Problem(statusCode: 400, title: "validation_error", detail: "order_sn and transaction_id are required");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var existing = await db.PaymentLogs.SingleOrDefaultAsync(x => x.Provider == provider && x.TransactionId == request.TransactionId, ct);
        if (existing is not null) return Results.Ok(new { accepted = true, duplicate = true });
        var order = await db.Orders.SingleOrDefaultAsync(x => x.OrderSn == request.OrderSn, ct); if (order is null) return Results.NotFound();
        if (order.OrderStatus == 2) return Results.Conflict(new { code = "order_cancelled" });
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        order.PayStatus = 2;
        db.PaymentLogs.Add(new PaymentLog { OrderId = order.OrderId, Provider = provider, TransactionId = request.TransactionId, PaidAt = now });
        db.OrderActions.Add(new OrderAction { OrderId = order.OrderId, UserId = order.UserId, ActionNote = $"payment_confirmed:{provider}", LogTime = now });
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Results.Ok(new { accepted = true, duplicate = false });
    }
}

public sealed record PaymentCallbackRequest(string OrderSn, string TransactionId);
