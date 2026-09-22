using Microsoft.EntityFrameworkCore;

namespace EcShop.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    private static readonly SemaphoreSlim InitializationLock = new(1, 1);

    public static async Task InitializeAsync(EcShopDbContext database, CancellationToken cancellationToken)
    {
        await InitializationLock.WaitAsync(cancellationToken);
        try
        {
            await database.Database.EnsureCreatedAsync(cancellationToken);
            if (database.Database.IsSqlite())
            {
                await EnsureLegacySqliteTablesAsync(database, cancellationToken);
            }
            if (await database.ShippingMethods.AnyAsync(cancellationToken) is false)
            {
                database.ShippingMethods.Add(new ShippingMethod { ShippingId = 1, ShippingName = "标准快递", ShippingFee = 8m });
            }

            if (await database.PaymentMethods.AnyAsync(cancellationToken) is false)
            {
                database.PaymentMethods.Add(new PaymentMethod { PayId = 1, PayName = "在线支付" });
            }

            if (await database.Goods.AnyAsync(cancellationToken) is false)
            {
                database.Categories.Add(new Category { CatId = 1, CatName = "示例分类" });
                database.Brands.Add(new Brand { BrandId = 1, BrandName = "示例品牌" });
                database.ArticleCategories.Add(new ArticleCategory { CatId = 1, CatName = "商城公告", CatDesc = "示例文章分类" });
                database.Articles.Add(new Article { ArticleId = 1, CatId = 1, Title = "C# ECSHOP 项目说明", Author = "csharp_ecshop", ArticleDesc = "用于验证文章 URL 的示例文章", Content = "这是来自数据库 ecs_article 表的示例正文。" });
                database.Regions.AddRange(new Region { RegionId = 1, ParentId = 0, RegionName = "中国", RegionType = 1 }, new Region { RegionId = 2, ParentId = 1, RegionName = "北京市", RegionType = 2 });
                database.Goods.AddRange(new Goods { GoodsId = 12, CatId = 1, BrandId = 1, GoodsSn = "CS-EC-001", GoodsName = "C# 入门商品", GoodsNumber = 18, MarketPrice = 69.90m, ShopPrice = 49.90m, GoodsBrief = "用于验证 HTTP URL 的示例商品", GoodsDesc = "来自 ecs_goods 表", IsBest = true, IsNew = true, AddTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() }, new Goods { GoodsId = 13, CatId = 1, BrandId = 1, GoodsSn = "CS-EC-002", GoodsName = "已下架商品", GoodsNumber = 5, MarketPrice = 129.90m, ShopPrice = 99.90m, IsOnSale = false });
                database.GoodsAttributes.AddRange(new GoodsOption { GoodsAttrId = 1001, GoodsId = 12, AttrId = 1, AttrValue = "扩展版", AttrPrice = 5m }, new GoodsOption { GoodsAttrId = 1005, GoodsId = 12, AttrId = 2, AttrValue = "标准包装", AttrPrice = 0m });
                database.Products.Add(new Product { ProductId = 81, GoodsId = 12, GoodsAttr = "1001|1005", ProductSn = "CS-EC-001-EXT", ProductNumber = 10 });
                await database.SaveChangesAsync(cancellationToken);
            }

            if (await database.GoodsGallery.AnyAsync(cancellationToken) is false) database.GoodsGallery.Add(new GoodsGalleryImage { ImageId = 1, GoodsId = 12, ImageUrl = "/images/goods-12.jpg", ThumbnailUrl = "/images/goods-12-thumb.jpg", OriginalUrl = "/images/goods-12-original.jpg", ImageDescription = "示例商品图" });
            if (await database.GoodsTags.AnyAsync(cancellationToken) is false) database.GoodsTags.AddRange(new GoodsTag { GoodsId = 12, TagWords = "C#" }, new GoodsTag { GoodsId = 12, TagWords = "入门" });
            if (await database.Topics.AnyAsync(cancellationToken) is false) database.Topics.Add(new ShopTopic { TopicId = 1, Title = "C# ECSHOP 专题", Intro = "用于专题 URL 集中验收", StartTime = 0, EndTime = 4_102_444_800 });
            if (await database.Votes.AnyAsync(cancellationToken) is false)
            {
                database.Votes.Add(new ShopVote { VoteId = 1, VoteName = "示例投票", StartTime = 0, EndTime = 4_102_444_800, CanMulti = false });
                database.VoteOptions.AddRange(new ShopVoteOption { OptionId = 1, VoteId = 1, OptionName = "满意", OptionOrder = 1 }, new ShopVoteOption { OptionId = 2, VoteId = 1, OptionName = "需要改进", OptionOrder = 2 });
            }
            if (await database.ExchangeGoods.AnyAsync(cancellationToken) is false) database.ExchangeGoods.Add(new ExchangeGoods { GoodsId = 12, ExchangeIntegral = 4990, IsHot = true });
            if (await database.WholesaleOffers.AnyAsync(cancellationToken) is false) database.WholesaleOffers.Add(new WholesaleOffer { ActivityId = 1, GoodsId = 12, GoodsName = "C# 入门商品", RankIds = "", Prices = "[{\"quantity\":10,\"price\":\"39.90\"}]" });
            var package = await database.GoodsActivities.OrderBy(x => x.ActivityId).FirstOrDefaultAsync(x => x.ActivityType == "package", cancellationToken);
            if (package is null)
            {
                package = new GoodsActivity { ActivityType = "package", ActivityName = "示例礼包", GoodsId = 12, StartTime = 0, EndTime = 4_102_444_800 };
                database.GoodsActivities.Add(package);
                await database.SaveChangesAsync(cancellationToken);
            }
            if (await database.PackageGoods.AnyAsync(cancellationToken) is false) database.PackageGoods.Add(new PackageGoods { PackageId = package.ActivityId, GoodsId = 12, ProductId = 0, GoodsNumber = 1 });
            await database.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            InitializationLock.Release();
        }
    }

    private static async Task EnsureLegacySqliteTablesAsync(EcShopDbContext database, CancellationToken cancellationToken)
    {
        await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ecs_users (user_id INTEGER PRIMARY KEY AUTOINCREMENT, user_name TEXT NOT NULL UNIQUE, email TEXT NOT NULL UNIQUE, password_hash TEXT NOT NULL)", cancellationToken);
        await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ecs_sessions (token_hash TEXT PRIMARY KEY, user_id INTEGER NOT NULL)", cancellationToken);
        await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ecs_user_address (address_id INTEGER PRIMARY KEY AUTOINCREMENT, user_id INTEGER NOT NULL, consignee TEXT NOT NULL, country_id INTEGER NOT NULL DEFAULT 0, province_id INTEGER NOT NULL DEFAULT 0, city_id INTEGER NOT NULL DEFAULT 0, district_id INTEGER NOT NULL DEFAULT 0, address TEXT NOT NULL, zipcode TEXT NOT NULL DEFAULT '', mobile TEXT NOT NULL DEFAULT '', is_default INTEGER NOT NULL DEFAULT 0)", cancellationToken);
        await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ecs_cart (rec_id INTEGER PRIMARY KEY AUTOINCREMENT, user_id INTEGER NOT NULL, goods_id INTEGER NOT NULL, goods_number INTEGER NOT NULL, version INTEGER NOT NULL DEFAULT 1, UNIQUE(user_id, goods_id))", cancellationToken);
        await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ecs_shipping (shipping_id INTEGER PRIMARY KEY, shipping_name TEXT NOT NULL, enabled INTEGER NOT NULL DEFAULT 1, shipping_fee DECIMAL NOT NULL DEFAULT 0)", cancellationToken);
        await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ecs_payment (pay_id INTEGER PRIMARY KEY, pay_name TEXT NOT NULL, enabled INTEGER NOT NULL DEFAULT 1, pay_fee DECIMAL NOT NULL DEFAULT 0)", cancellationToken);
        await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ecs_order_info (order_id INTEGER PRIMARY KEY AUTOINCREMENT, order_sn TEXT NOT NULL UNIQUE, user_id INTEGER NOT NULL, order_status INTEGER NOT NULL DEFAULT 0, pay_status INTEGER NOT NULL DEFAULT 0, shipping_status INTEGER NOT NULL DEFAULT 0, consignee TEXT NOT NULL, address TEXT NOT NULL, shipping_id INTEGER NOT NULL, shipping_name TEXT NOT NULL, pay_id INTEGER NOT NULL, pay_name TEXT NOT NULL, goods_amount DECIMAL NOT NULL, shipping_fee DECIMAL NOT NULL, pay_fee DECIMAL NOT NULL, order_amount DECIMAL NOT NULL, add_time INTEGER NOT NULL, client_request_id TEXT NOT NULL, UNIQUE(user_id, client_request_id))", cancellationToken);
        await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ecs_order_goods (rec_id INTEGER PRIMARY KEY AUTOINCREMENT, order_id INTEGER NOT NULL, goods_id INTEGER NOT NULL, goods_name TEXT NOT NULL, goods_sn TEXT NOT NULL, goods_price DECIMAL NOT NULL, goods_number INTEGER NOT NULL)", cancellationToken);
        await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ecs_order_action (action_id INTEGER PRIMARY KEY AUTOINCREMENT, order_id INTEGER NOT NULL, user_id INTEGER NOT NULL, action_note TEXT NOT NULL, log_time INTEGER NOT NULL)", cancellationToken);
        await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ecs_comment (comment_id INTEGER PRIMARY KEY AUTOINCREMENT, goods_id INTEGER NOT NULL, user_id INTEGER NOT NULL, content TEXT NOT NULL, is_visible INTEGER NOT NULL DEFAULT 1, add_time INTEGER NOT NULL)", cancellationToken);
        await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ecs_pay_log (log_id INTEGER PRIMARY KEY AUTOINCREMENT, order_id INTEGER NOT NULL, provider TEXT NOT NULL, transaction_id TEXT NOT NULL, paid_at INTEGER NOT NULL, UNIQUE(provider, transaction_id))", cancellationToken);
        await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ecs_goods_activity (act_id INTEGER PRIMARY KEY AUTOINCREMENT, act_type TEXT NOT NULL, act_name TEXT NOT NULL, goods_id INTEGER NOT NULL, start_time INTEGER NOT NULL, end_time INTEGER NOT NULL, is_finished INTEGER NOT NULL DEFAULT 0)", cancellationToken);
        await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ecs_goods_gallery (img_id INTEGER PRIMARY KEY AUTOINCREMENT, goods_id INTEGER NOT NULL, img_url TEXT NOT NULL, img_desc TEXT NOT NULL DEFAULT '', thumb_url TEXT NOT NULL DEFAULT '', img_original TEXT NOT NULL DEFAULT '')", cancellationToken);
        await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ecs_feedback (msg_id INTEGER PRIMARY KEY AUTOINCREMENT, parent_id INTEGER NOT NULL DEFAULT 0, user_id INTEGER NOT NULL DEFAULT 0, user_name TEXT NOT NULL DEFAULT '', user_email TEXT NOT NULL DEFAULT '', msg_title TEXT NOT NULL, msg_type INTEGER NOT NULL DEFAULT 0, msg_status INTEGER NOT NULL DEFAULT 1, msg_content TEXT NOT NULL, msg_time INTEGER NOT NULL, order_id INTEGER NOT NULL DEFAULT 0, msg_area INTEGER NOT NULL DEFAULT 0)", cancellationToken);
        await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ecs_tag (tag_id INTEGER PRIMARY KEY AUTOINCREMENT, user_id INTEGER NOT NULL DEFAULT 0, goods_id INTEGER NOT NULL, tag_words TEXT NOT NULL, UNIQUE(user_id, goods_id, tag_words))", cancellationToken);
        await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ecs_topic (topic_id INTEGER PRIMARY KEY AUTOINCREMENT, title TEXT NOT NULL, intro TEXT NOT NULL DEFAULT '', start_time INTEGER NOT NULL, end_time INTEGER NOT NULL)", cancellationToken);
        await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ecs_vote (vote_id INTEGER PRIMARY KEY AUTOINCREMENT, vote_name TEXT NOT NULL, start_time INTEGER NOT NULL, end_time INTEGER NOT NULL, can_multi INTEGER NOT NULL DEFAULT 0, vote_count INTEGER NOT NULL DEFAULT 0)", cancellationToken);
        await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ecs_vote_option (option_id INTEGER PRIMARY KEY AUTOINCREMENT, vote_id INTEGER NOT NULL, option_name TEXT NOT NULL, option_count INTEGER NOT NULL DEFAULT 0, option_order INTEGER NOT NULL DEFAULT 100)", cancellationToken);
        await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ecs_vote_log (log_id INTEGER PRIMARY KEY AUTOINCREMENT, vote_id INTEGER NOT NULL, user_id INTEGER NOT NULL, vote_time INTEGER NOT NULL, UNIQUE(vote_id, user_id))", cancellationToken);
        await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ecs_wholesale (act_id INTEGER PRIMARY KEY AUTOINCREMENT, goods_id INTEGER NOT NULL, goods_name TEXT NOT NULL, rank_ids TEXT NOT NULL DEFAULT '', prices TEXT NOT NULL, enabled INTEGER NOT NULL DEFAULT 1)", cancellationToken);
        await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ecs_exchange_goods (goods_id INTEGER PRIMARY KEY, exchange_integral INTEGER NOT NULL DEFAULT 0, is_exchange INTEGER NOT NULL DEFAULT 1, is_hot INTEGER NOT NULL DEFAULT 0)", cancellationToken);
        await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ecs_package_goods (package_id INTEGER NOT NULL, goods_id INTEGER NOT NULL, product_id INTEGER NOT NULL DEFAULT 0, goods_number INTEGER NOT NULL DEFAULT 1, PRIMARY KEY(package_id, goods_id, product_id))", cancellationToken);
    }
}
