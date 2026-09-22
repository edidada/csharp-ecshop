using EcShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EcShop.Api.Endpoints;

public static class CommentEndpoints
{
    public static IEndpointRouteBuilder MapCommentEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/v1/goods/{id:long}/comments", ListAsync);
        routes.MapPost("/api/v1/goods/{id:long}/comments", CreateAsync);
        return routes;
    }

    private static async Task<IResult> ListAsync(long id, HttpRequest http, EcShopDbContext db, CancellationToken ct)
    {
        if (!await db.Goods.AnyAsync(x => x.GoodsId == id && x.IsOnSale && !x.IsDelete, ct)) return Results.NotFound();
        var page = int.TryParse(http.Query["page"], out var queryPage) ? Math.Max(queryPage, 1) : 1;
        var pageSize = int.TryParse(http.Query["page_size"], out var queryPageSize) ? Math.Clamp(queryPageSize, 1, 100) : 20;
        var query = db.Comments.Where(x => x.GoodsId == id && x.IsVisible).OrderByDescending(x => x.CommentId);
        var total = await query.CountAsync(ct); var comments = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var userNames = await db.Users.Where(x => comments.Select(comment => comment.UserId).Contains(x.UserId)).ToDictionaryAsync(x => x.UserId, x => x.UserName, ct);
        return Results.Ok(new { page, page_size = pageSize, total, items = comments.Select(x => new { id = x.CommentId, user_id = x.UserId, username = userNames.GetValueOrDefault(x.UserId, "匿名用户"), content = x.Content, created_at = x.AddTime }) });
    }

    private static async Task<IResult> CreateAsync(long id, CreateCommentRequest request, HttpRequest http, EcShopDbContext db, CancellationToken ct)
    {
        var user = await IdentityEndpoints.CurrentUserAsync(http, db, ct); if (user is null) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Content) || request.Content.Length > 500) return Results.Problem(statusCode: 400, title: "validation_error", detail: "content must be between 1 and 500 characters");
        if (!await db.Goods.AnyAsync(x => x.GoodsId == id && x.IsOnSale && !x.IsDelete, ct)) return Results.NotFound();
        var comment = new ShopComment { GoodsId = id, UserId = user.UserId, Content = request.Content.Trim(), AddTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
        db.Comments.Add(comment); await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/goods/{id}/comments/{comment.CommentId}", new { id = comment.CommentId, content = comment.Content, created_at = comment.AddTime });
    }
}

public sealed record CreateCommentRequest(string Content);
