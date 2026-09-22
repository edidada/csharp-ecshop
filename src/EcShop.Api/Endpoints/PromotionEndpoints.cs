using EcShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EcShop.Api.Endpoints;

public static class PromotionEndpoints
{
    private static readonly string[] SupportedKinds = ["group-buy", "auction", "snatch"];

    public static IEndpointRouteBuilder MapPromotionEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api/v1/promotions");
        api.MapGet("/{kind}", ListAsync);
        api.MapPost("/{kind}", CreateAsync);
        return routes;
    }

    private static async Task<IResult> ListAsync(string kind, EcShopDbContext db, CancellationToken ct)
    {
        if (!SupportedKinds.Contains(kind, StringComparer.Ordinal)) return Results.NotFound();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var activities = await db.GoodsActivities.Where(x => x.ActivityType == kind && !x.IsFinished && x.StartTime <= now && x.EndTime >= now).OrderBy(x => x.EndTime).ToListAsync(ct);
        return Results.Ok(new { items = activities.Select(Activity) });
    }

    private static async Task<IResult> CreateAsync(string kind, CreatePromotionRequest request, HttpRequest http, EcShopDbContext db, CancellationToken ct)
    {
        if (!SupportedKinds.Contains(kind, StringComparer.Ordinal)) return Results.NotFound();
        if (await IdentityEndpoints.CurrentUserAsync(http, db, ct) is null) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Name) || request.GoodsId <= 0 || request.EndTime <= request.StartTime || !await db.Goods.AnyAsync(x => x.GoodsId == request.GoodsId && x.IsOnSale && !x.IsDelete, ct)) return Results.Problem(statusCode: 400, title: "validation_error", detail: "a visible good, name and valid time range are required");
        var activity = new GoodsActivity { ActivityType = kind, ActivityName = request.Name.Trim(), GoodsId = request.GoodsId, StartTime = request.StartTime, EndTime = request.EndTime };
        db.GoodsActivities.Add(activity); await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/promotions/{kind}/{activity.ActivityId}", Activity(activity));
    }

    private static object Activity(GoodsActivity activity) => new { id = activity.ActivityId, type = activity.ActivityType, name = activity.ActivityName, goods_id = activity.GoodsId, start_time = activity.StartTime, end_time = activity.EndTime };
}

public sealed record CreatePromotionRequest(string Name, long GoodsId, long StartTime, long EndTime);
