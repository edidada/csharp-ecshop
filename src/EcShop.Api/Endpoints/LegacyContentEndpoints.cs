using EcShop.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Security.Cryptography;

namespace EcShop.Api.Endpoints;

public static class LegacyContentEndpoints
{
    private static readonly string[] CompatibilityCapabilities = ["catalog", "checkout", "content", "promotions"];

    public static IEndpointRouteBuilder MapLegacyContentEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api/v1");
        api.MapGet("/activities", ActivitiesAsync);
        api.MapGet("/announcements", AnnouncementsAsync);
        api.MapGet("/compat/status", () => Results.Ok(new { api = "csharp-ecshop", version = "v1", capabilities = CompatibilityCapabilities }));
        api.MapGet("/captcha", CreateCaptcha);
        api.MapPost("/captcha/verify", VerifyCaptcha);
        api.MapGet("/compare", CompareAsync);
        api.MapGet("/exchange-goods", ExchangeGoodsAsync);
        api.MapGet("/feed", FeedAsync);
        api.MapGet("/goods/{id:long}/gallery", GalleryAsync);
        api.MapGet("/goods/{id:long}/tags", GoodsTagsAsync);
        api.MapPost("/goods/{id:long}/tags", AddGoodsTagsAsync);
        api.MapGet("/messages", MessagesAsync);
        api.MapPost("/messages", AddMessageAsync);
        api.MapGet("/packages", PackagesAsync);
        api.MapGet("/tags", TagsAsync);
        api.MapGet("/topics/{id:long}", TopicAsync);
        api.MapGet("/votes/{id:long}", VoteAsync);
        api.MapPost("/votes/{id:long}", SubmitVoteAsync);
        api.MapGet("/wholesale", WholesaleAsync);
        return routes;
    }

    private static async Task<IResult> ActivitiesAsync(EcShopDbContext db, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var rows = await db.GoodsActivities.AsNoTracking().Where(x => !x.IsFinished && x.StartTime <= now && x.EndTime >= now).OrderBy(x => x.EndTime).ToListAsync(ct);
        return Results.Ok(new { items = rows.Select(Activity) });
    }

    private static async Task<IResult> AnnouncementsAsync(EcShopDbContext db, CancellationToken ct)
    {
        var rows = await db.Articles.AsNoTracking().Where(x => x.IsOpen).OrderByDescending(x => x.ArticleId).Take(20).ToListAsync(ct);
        return Results.Ok(new { items = rows.Select(x => new { id = x.ArticleId, title = x.Title, summary = x.ArticleDesc, content = x.Content }) });
    }

    private static IResult CreateCaptcha(IMemoryCache cache)
    {
        var left = RandomNumberGenerator.GetInt32(1, 10); var right = RandomNumberGenerator.GetInt32(1, 10); var challengeId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        cache.Set(CaptchaKey(challengeId), left + right, TimeSpan.FromMinutes(5));
        return Results.Ok(new { challenge_id = challengeId, question = $"{left} + {right}", expires_in = 300 });
    }

    private static IResult VerifyCaptcha(CaptchaVerificationRequest request, IMemoryCache cache)
    {
        if (string.IsNullOrWhiteSpace(request.ChallengeId) || !cache.TryGetValue<int>(CaptchaKey(request.ChallengeId), out var answer)) return Results.Problem(statusCode: 400, title: "captcha_expired", detail: "captcha challenge is missing or expired");
        cache.Remove(CaptchaKey(request.ChallengeId));
        return request.Answer == answer ? Results.Ok(new { valid = true }) : Results.Problem(statusCode: 400, title: "captcha_invalid", detail: "captcha answer is invalid");
    }

    private static async Task<IResult> CompareAsync([FromQuery(Name = "goods_ids")] string? goodsIds, EcShopDbContext db, CancellationToken ct)
    {
        var ids = (goodsIds ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(value => long.TryParse(value, out var id) ? id : 0).Where(id => id > 0).Distinct().Take(5).ToArray();
        if (ids.Length == 0) return Validation("goods_ids must contain between one and five goods IDs");
        var rows = await db.Goods.AsNoTracking().Where(x => ids.Contains(x.GoodsId) && x.IsOnSale && !x.IsDelete).ToListAsync(ct);
        if (rows.Count != ids.Length) return Results.NotFound();
        var ordered = ids.Select(id => rows.Single(x => x.GoodsId == id));
        return Results.Ok(new { items = ordered.Select(GoodsSummary) });
    }

    private static async Task<IResult> ExchangeGoodsAsync(EcShopDbContext db, CancellationToken ct)
    {
        var rows = await (from exchange in db.ExchangeGoods.AsNoTracking() join goods in db.Goods.AsNoTracking() on exchange.GoodsId equals goods.GoodsId where exchange.IsExchange && goods.IsOnSale && !goods.IsDelete orderby exchange.IsHot descending, goods.GoodsId select new { exchange, goods }).ToListAsync(ct);
        return Results.Ok(new { items = rows.Select(x => new { goods = GoodsSummary(x.goods), exchange_integral = x.exchange.ExchangeIntegral, is_hot = x.exchange.IsHot }) });
    }

    private static async Task<IResult> FeedAsync(EcShopDbContext db, CancellationToken ct)
    {
        var goods = await db.Goods.AsNoTracking().Where(x => x.IsOnSale && !x.IsDelete).OrderByDescending(x => x.AddTime).Take(20).ToListAsync(ct);
        var articles = await db.Articles.AsNoTracking().Where(x => x.IsOpen).OrderByDescending(x => x.ArticleId).Take(20).ToListAsync(ct);
        return Results.Ok(new { title = "csharp-ecshop", goods = goods.Select(GoodsSummary), articles = articles.Select(x => new { id = x.ArticleId, title = x.Title, summary = x.ArticleDesc }) });
    }

    private static async Task<IResult> GalleryAsync(long id, EcShopDbContext db, CancellationToken ct)
    {
        if (!await VisibleGoods(db).AnyAsync(x => x.GoodsId == id, ct)) return Results.NotFound();
        var rows = await db.GoodsGallery.AsNoTracking().Where(x => x.GoodsId == id).OrderBy(x => x.ImageId).ToListAsync(ct);
        return Results.Ok(new { items = rows.Select(x => new { id = x.ImageId, image_url = x.ImageUrl, description = x.ImageDescription, thumbnail_url = x.ThumbnailUrl, original_url = x.OriginalUrl }) });
    }

    private static async Task<IResult> MessagesAsync(HttpRequest http, EcShopDbContext db, CancellationToken ct)
    {
        var (page, size) = Page(http); var query = db.FeedbackMessages.AsNoTracking().Where(x => x.IsVisible).OrderByDescending(x => x.MessageId); var total = await query.CountAsync(ct); var rows = await query.Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return Results.Ok(new { page, page_size = size, total, items = rows.Select(Message) });
    }

    private static async Task<IResult> AddMessageAsync(CreateMessageRequest request, HttpRequest http, EcShopDbContext db, CancellationToken ct)
    {
        var user = await IdentityEndpoints.CurrentUserAsync(http, db, ct); if (user is null) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content) || request.Title.Length > 200 || request.Content.Length > 2000) return Validation("title and content are required and must fit their limits");
        var message = new FeedbackMessage { UserId = user.UserId, UserName = user.UserName, UserEmail = user.Email, Title = request.Title.Trim(), Content = request.Content.Trim(), MessageType = request.Type, IsVisible = true, MessageTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), OrderId = request.OrderId };
        db.FeedbackMessages.Add(message); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/messages/{message.MessageId}", Message(message));
    }

    private static async Task<IResult> PackagesAsync(EcShopDbContext db, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); var packages = await db.GoodsActivities.AsNoTracking().Where(x => x.ActivityType == "package" && !x.IsFinished && x.StartTime <= now && x.EndTime >= now).OrderBy(x => x.ActivityId).ToListAsync(ct); var packageIds = packages.Select(x => x.ActivityId).ToArray();
        var items = await db.PackageGoods.AsNoTracking().Where(x => packageIds.Contains(x.PackageId)).ToListAsync(ct); var goodsIds = items.Select(x => x.GoodsId).Distinct().ToArray(); var goods = await db.Goods.AsNoTracking().Where(x => goodsIds.Contains(x.GoodsId) && x.IsOnSale && !x.IsDelete).ToDictionaryAsync(x => x.GoodsId, ct);
        return Results.Ok(new { items = packages.Select(package => new { id = package.ActivityId, name = package.ActivityName, goods = items.Where(x => x.PackageId == package.ActivityId && goods.ContainsKey(x.GoodsId)).Select(x => new { goods = GoodsSummary(goods[x.GoodsId]), quantity = x.GoodsNumber, product_id = x.ProductId }) }) });
    }

    private static async Task<IResult> TagsAsync(EcShopDbContext db, CancellationToken ct)
    {
        var rows = await db.GoodsTags.AsNoTracking().GroupBy(x => x.TagWords).Select(group => new { word = group.Key, count = group.Count() }).OrderByDescending(x => x.count).ThenBy(x => x.word).Take(100).ToListAsync(ct);
        return Results.Ok(new { items = rows });
    }

    private static async Task<IResult> GoodsTagsAsync(long id, EcShopDbContext db, CancellationToken ct)
    {
        if (!await VisibleGoods(db).AnyAsync(x => x.GoodsId == id, ct)) return Results.NotFound();
        var rows = await db.GoodsTags.AsNoTracking().Where(x => x.GoodsId == id).GroupBy(x => x.TagWords).Select(group => new { word = group.Key, count = group.Count() }).OrderByDescending(x => x.count).ThenBy(x => x.word).ToListAsync(ct);
        return Results.Ok(new { items = rows });
    }

    private static async Task<IResult> AddGoodsTagsAsync(long id, AddGoodsTagsRequest request, HttpRequest http, EcShopDbContext db, CancellationToken ct)
    {
        var user = await IdentityEndpoints.CurrentUserAsync(http, db, ct); if (user is null) return Results.Unauthorized(); if (!await VisibleGoods(db).AnyAsync(x => x.GoodsId == id, ct)) return Results.NotFound();
        var words = (request.Tag ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(x => x.Length is > 0 and <= 40).Distinct(StringComparer.OrdinalIgnoreCase).Take(10).ToArray(); if (words.Length == 0) return Validation("tag must contain at least one word");
        var existing = await db.GoodsTags.Where(x => x.UserId == user.UserId && x.GoodsId == id && words.Contains(x.TagWords)).Select(x => x.TagWords).ToListAsync(ct); db.GoodsTags.AddRange(words.Except(existing, StringComparer.OrdinalIgnoreCase).Select(word => new GoodsTag { UserId = user.UserId, GoodsId = id, TagWords = word })); await db.SaveChangesAsync(ct);
        return Results.Ok(new { items = words });
    }

    private static async Task<IResult> TopicAsync(long id, EcShopDbContext db, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); var topic = await db.Topics.AsNoTracking().SingleOrDefaultAsync(x => x.TopicId == id && x.StartTime <= now && x.EndTime >= now, ct); return topic is null ? Results.NotFound() : Results.Ok(new { id = topic.TopicId, title = topic.Title, intro = topic.Intro, start_time = topic.StartTime, end_time = topic.EndTime });
    }

    private static async Task<IResult> VoteAsync(long id, EcShopDbContext db, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); var vote = await db.Votes.AsNoTracking().SingleOrDefaultAsync(x => x.VoteId == id && x.StartTime <= now && x.EndTime >= now, ct); if (vote is null) return Results.NotFound(); var options = await db.VoteOptions.AsNoTracking().Where(x => x.VoteId == id).OrderBy(x => x.OptionOrder).ThenBy(x => x.OptionId).ToListAsync(ct); return Results.Ok(Vote(vote, options));
    }

    private static async Task<IResult> SubmitVoteAsync(long id, SubmitVoteRequest request, HttpRequest http, EcShopDbContext db, CancellationToken ct)
    {
        var user = await IdentityEndpoints.CurrentUserAsync(http, db, ct); if (user is null) return Results.Unauthorized(); var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); var vote = await db.Votes.AsNoTracking().SingleOrDefaultAsync(x => x.VoteId == id && x.StartTime <= now && x.EndTime >= now, ct); if (vote is null) return Results.NotFound(); if (await db.VoteLogs.AnyAsync(x => x.VoteId == id && x.UserId == user.UserId, ct)) return Results.Conflict(new { code = "already_voted" });
        var optionIds = request.OptionIds?.Distinct().ToArray() ?? []; if (optionIds.Length == 0 || (!vote.CanMulti && optionIds.Length != 1)) return Validation(vote.CanMulti ? "optionIds is required" : "exactly one option is required"); var validOptions = await db.VoteOptions.AsNoTracking().Where(x => x.VoteId == id && optionIds.Contains(x.OptionId)).Select(x => x.OptionId).ToListAsync(ct); if (validOptions.Count != optionIds.Length) return Validation("optionIds contains an invalid option");
        await using var transaction = await db.Database.BeginTransactionAsync(ct); foreach (var optionId in optionIds) await db.VoteOptions.Where(x => x.OptionId == optionId && x.VoteId == id).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.OptionCount, x => x.OptionCount + 1), ct); await db.Votes.Where(x => x.VoteId == id).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.VoteCount, x => x.VoteCount + 1), ct); db.VoteLogs.Add(new ShopVoteLog { VoteId = id, UserId = user.UserId, VoteTime = now }); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return Results.Ok(new { accepted = true });
    }

    private static async Task<IResult> WholesaleAsync(EcShopDbContext db, CancellationToken ct)
    {
        var rows = await (from offer in db.WholesaleOffers.AsNoTracking() join goods in db.Goods.AsNoTracking() on offer.GoodsId equals goods.GoodsId where offer.Enabled && goods.IsOnSale && !goods.IsDelete orderby offer.ActivityId select new { offer, goods }).ToListAsync(ct); return Results.Ok(new { items = rows.Select(x => new { id = x.offer.ActivityId, goods = GoodsSummary(x.goods), rank_ids = x.offer.RankIds, prices = x.offer.Prices }) });
    }

    private static IQueryable<Goods> VisibleGoods(EcShopDbContext db) => db.Goods.AsNoTracking().Where(x => x.IsOnSale && !x.IsDelete);
    private static object GoodsSummary(Goods goods) => new { id = goods.GoodsId, name = goods.GoodsName, price = goods.ShopPrice.ToString("0.00", CultureInfo.InvariantCulture), stock_available = goods.GoodsNumber };
    private static object Activity(GoodsActivity activity) => new { id = activity.ActivityId, type = activity.ActivityType, name = activity.ActivityName, goods_id = activity.GoodsId, start_time = activity.StartTime, end_time = activity.EndTime };
    private static object Message(FeedbackMessage message) => new { id = message.MessageId, parent_id = message.ParentId, user_name = message.UserName, title = message.Title, type = message.MessageType, content = message.Content, order_id = message.OrderId, created_at = message.MessageTime };
    private static object Vote(ShopVote vote, IEnumerable<ShopVoteOption> options) => new { id = vote.VoteId, name = vote.VoteName, can_multi = vote.CanMulti, vote_count = vote.VoteCount, options = options.Select(x => new { id = x.OptionId, name = x.OptionName, count = x.OptionCount }) };
    private static (int page, int size) Page(HttpRequest http) { var page = int.TryParse(http.Query["page"], out var parsedPage) ? Math.Max(parsedPage, 1) : 1; var size = int.TryParse(http.Query["page_size"], out var parsedSize) ? Math.Clamp(parsedSize, 1, 100) : 20; return (page, size); }
    private static string CaptchaKey(string challengeId) => $"captcha:{challengeId}";
    private static IResult Validation(string detail) => Results.Problem(statusCode: 400, title: "validation_error", detail: detail);
}

public sealed record CaptchaVerificationRequest(string? ChallengeId, int Answer);
public sealed record CreateMessageRequest(string? Title, string? Content, int Type, long OrderId);
public sealed record AddGoodsTagsRequest(string? Tag);
public sealed record SubmitVoteRequest(long[]? OptionIds);
