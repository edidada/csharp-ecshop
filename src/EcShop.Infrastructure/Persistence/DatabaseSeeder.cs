using Microsoft.EntityFrameworkCore;

namespace EcShop.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    private static readonly SemaphoreSlim InitializationLock = new(1, 1);

    public static async Task InitializeAsync(EcShopDbContext database, CancellationToken cancellationToken)
    {
        if (!database.Database.IsSqlite()) return;
        await InitializationLock.WaitAsync(cancellationToken);
        try
        {
            await database.Database.EnsureCreatedAsync(cancellationToken);
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
            if (await database.ShippingMethods.AnyAsync(cancellationToken) is false)
            {
                database.ShippingMethods.Add(new ShippingMethod { ShippingId = 1, ShippingName = "标准快递", ShippingFee = 8m });
            }

            if (await database.PaymentMethods.AnyAsync(cancellationToken) is false)
            {
                database.PaymentMethods.Add(new PaymentMethod { PayId = 1, PayName = "在线支付" });
            }

            if (await database.Goods.AnyAsync(cancellationToken))
            {
                await database.SaveChangesAsync(cancellationToken);
                return;
            }

            database.Categories.Add(new Category { CatId = 1, CatName = "示例分类" });
            database.Brands.Add(new Brand { BrandId = 1, BrandName = "示例品牌" });
            database.ArticleCategories.Add(new ArticleCategory { CatId = 1, CatName = "商城公告", CatDesc = "示例文章分类" });
            database.Articles.Add(new Article { ArticleId = 1, CatId = 1, Title = "C# ECSHOP 项目说明", Author = "csharp_ecshop", ArticleDesc = "用于验证文章 URL 的示例文章", Content = "这是来自 SQLite ecs_article 表的示例正文。" });
            database.Regions.AddRange(new Region { RegionId = 1, ParentId = 0, RegionName = "中国", RegionType = 1 }, new Region { RegionId = 2, ParentId = 1, RegionName = "北京市", RegionType = 2 });
            database.Goods.AddRange(new Goods { GoodsId = 12, CatId = 1, BrandId = 1, GoodsSn = "CS-EC-001", GoodsName = "C# 入门商品", GoodsNumber = 18, MarketPrice = 69.90m, ShopPrice = 49.90m, GoodsBrief = "用于验证第一个 HTTP URL 的示例商品", GoodsDesc = "来自 SQLite ecs_goods 表", IsBest = true, IsNew = true, AddTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() }, new Goods { GoodsId = 13, CatId = 1, BrandId = 1, GoodsSn = "CS-EC-002", GoodsName = "已下架商品", GoodsNumber = 5, MarketPrice = 129.90m, ShopPrice = 99.90m, IsOnSale = false });
            database.GoodsAttributes.AddRange(new GoodsOption { GoodsAttrId = 1001, GoodsId = 12, AttrId = 1, AttrValue = "扩展版", AttrPrice = 5m }, new GoodsOption { GoodsAttrId = 1005, GoodsId = 12, AttrId = 2, AttrValue = "标准包装", AttrPrice = 0m });
            database.Products.Add(new Product { ProductId = 81, GoodsId = 12, GoodsAttr = "1001|1005", ProductSn = "CS-EC-001-EXT", ProductNumber = 10 });
            await database.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            InitializationLock.Release();
        }
    }
}
