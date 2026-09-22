-- SQLite development schema. Original ECSHOP table and column names are retained.
PRAGMA foreign_keys = ON;
BEGIN;
CREATE TABLE IF NOT EXISTS ecs_category (
 cat_id INTEGER PRIMARY KEY, cat_name VARCHAR(90) NOT NULL DEFAULT '', keywords VARCHAR(255) NOT NULL DEFAULT '', cat_desc VARCHAR(255) NOT NULL DEFAULT '', parent_id INTEGER NOT NULL DEFAULT 0, sort_order INTEGER NOT NULL DEFAULT 50, template_file VARCHAR(50) NOT NULL DEFAULT '', measure_unit VARCHAR(15) NOT NULL DEFAULT '', show_in_nav INTEGER NOT NULL DEFAULT 0, style VARCHAR(150) NOT NULL DEFAULT '', is_show INTEGER NOT NULL DEFAULT 1, grade INTEGER NOT NULL DEFAULT 0, filter_attr VARCHAR(255) NOT NULL DEFAULT '0');
CREATE TABLE IF NOT EXISTS ecs_brand (
 brand_id INTEGER PRIMARY KEY, brand_name VARCHAR(60) NOT NULL DEFAULT '', brand_logo VARCHAR(80) NOT NULL DEFAULT '', brand_desc TEXT NOT NULL DEFAULT '', site_url VARCHAR(255) NOT NULL DEFAULT '', sort_order INTEGER NOT NULL DEFAULT 50, is_show INTEGER NOT NULL DEFAULT 1);
CREATE TABLE IF NOT EXISTS ecs_goods (
 goods_id INTEGER PRIMARY KEY, cat_id INTEGER NOT NULL DEFAULT 0, goods_sn VARCHAR(60) NOT NULL DEFAULT '', goods_name VARCHAR(120) NOT NULL DEFAULT '', goods_name_style VARCHAR(60) NOT NULL DEFAULT '+', click_count INTEGER NOT NULL DEFAULT 0, brand_id INTEGER NOT NULL DEFAULT 0, provider_name VARCHAR(100) NOT NULL DEFAULT '', goods_number INTEGER NOT NULL DEFAULT 0, goods_weight DECIMAL(10,3) NOT NULL DEFAULT 0.000, market_price DECIMAL(10,2) NOT NULL DEFAULT 0.00, shop_price DECIMAL(10,2) NOT NULL DEFAULT 0.00, promote_price DECIMAL(10,2) NOT NULL DEFAULT 0.00, promote_start_date INTEGER NOT NULL DEFAULT 0, promote_end_date INTEGER NOT NULL DEFAULT 0, warn_number INTEGER NOT NULL DEFAULT 1, keywords VARCHAR(255) NOT NULL DEFAULT '', goods_brief VARCHAR(255) NOT NULL DEFAULT '', goods_desc TEXT NOT NULL DEFAULT '', goods_thumb VARCHAR(255) NOT NULL DEFAULT '', goods_img VARCHAR(255) NOT NULL DEFAULT '', original_img VARCHAR(255) NOT NULL DEFAULT '', is_real INTEGER NOT NULL DEFAULT 1, extension_code VARCHAR(30) NOT NULL DEFAULT '', is_on_sale INTEGER NOT NULL DEFAULT 1, is_alone_sale INTEGER NOT NULL DEFAULT 1, is_shipping INTEGER NOT NULL DEFAULT 0, integral INTEGER NOT NULL DEFAULT 0, add_time INTEGER NOT NULL DEFAULT 0, sort_order INTEGER NOT NULL DEFAULT 100, is_delete INTEGER NOT NULL DEFAULT 0, is_best INTEGER NOT NULL DEFAULT 0, is_new INTEGER NOT NULL DEFAULT 0, is_hot INTEGER NOT NULL DEFAULT 0, is_promote INTEGER NOT NULL DEFAULT 0, bonus_type_id INTEGER NOT NULL DEFAULT 0, last_update INTEGER NOT NULL DEFAULT 0, goods_type INTEGER NOT NULL DEFAULT 0, seller_note VARCHAR(255) NOT NULL DEFAULT '', give_integral INTEGER NOT NULL DEFAULT -1, rank_integral INTEGER NOT NULL DEFAULT -1, suppliers_id INTEGER, is_check INTEGER,
 FOREIGN KEY(cat_id) REFERENCES ecs_category(cat_id), FOREIGN KEY(brand_id) REFERENCES ecs_brand(brand_id));
CREATE INDEX IF NOT EXISTS idx_ecs_goods_visibility ON ecs_goods(goods_id, is_on_sale, is_delete);
CREATE TABLE IF NOT EXISTS ecs_products (
 product_id INTEGER PRIMARY KEY, goods_id INTEGER NOT NULL, goods_attr VARCHAR(255) NOT NULL DEFAULT '', product_sn VARCHAR(60) NOT NULL DEFAULT '', product_number INTEGER NOT NULL DEFAULT 0,
 FOREIGN KEY(goods_id) REFERENCES ecs_goods(goods_id));
CREATE TABLE IF NOT EXISTS ecs_goods_attr (
 goods_attr_id INTEGER PRIMARY KEY, goods_id INTEGER NOT NULL, attr_id INTEGER NOT NULL DEFAULT 0, attr_value TEXT NOT NULL DEFAULT '', attr_price VARCHAR(255) NOT NULL DEFAULT '0.00',
 FOREIGN KEY(goods_id) REFERENCES ecs_goods(goods_id));
CREATE TABLE IF NOT EXISTS ecs_article_cat (
 cat_id INTEGER PRIMARY KEY, cat_name VARCHAR(90) NOT NULL DEFAULT '', cat_desc TEXT NOT NULL DEFAULT '', keywords VARCHAR(255) NOT NULL DEFAULT '', sort_order INTEGER NOT NULL DEFAULT 50, is_show INTEGER NOT NULL DEFAULT 1);
CREATE TABLE IF NOT EXISTS ecs_article (
 article_id INTEGER PRIMARY KEY, cat_id INTEGER NOT NULL, title VARCHAR(255) NOT NULL DEFAULT '', author VARCHAR(255) NOT NULL DEFAULT '', article_desc TEXT NOT NULL DEFAULT '', content TEXT NOT NULL DEFAULT '', keywords VARCHAR(255) NOT NULL DEFAULT '', is_open INTEGER NOT NULL DEFAULT 1,
 FOREIGN KEY(cat_id) REFERENCES ecs_article_cat(cat_id));
CREATE TABLE IF NOT EXISTS ecs_region (
 region_id INTEGER PRIMARY KEY, parent_id INTEGER NOT NULL DEFAULT 0, region_name VARCHAR(120) NOT NULL, region_type INTEGER NOT NULL);
CREATE TABLE IF NOT EXISTS ecs_users (
 user_id INTEGER PRIMARY KEY AUTOINCREMENT, user_name VARCHAR(60) NOT NULL UNIQUE, email VARCHAR(120) NOT NULL UNIQUE, password_hash VARCHAR(255) NOT NULL, created_at INTEGER NOT NULL DEFAULT (unixepoch()));
CREATE TABLE IF NOT EXISTS ecs_sessions (
 token_hash CHAR(64) PRIMARY KEY, user_id INTEGER NOT NULL, created_at INTEGER NOT NULL DEFAULT (unixepoch()),
 FOREIGN KEY(user_id) REFERENCES ecs_users(user_id) ON DELETE CASCADE);
CREATE TABLE IF NOT EXISTS ecs_user_address (
 address_id INTEGER PRIMARY KEY AUTOINCREMENT, user_id INTEGER NOT NULL, consignee VARCHAR(60) NOT NULL, country_id INTEGER NOT NULL DEFAULT 0, province_id INTEGER NOT NULL DEFAULT 0, city_id INTEGER NOT NULL DEFAULT 0, district_id INTEGER NOT NULL DEFAULT 0, address VARCHAR(255) NOT NULL, zipcode VARCHAR(20) NOT NULL DEFAULT '', mobile VARCHAR(32) NOT NULL DEFAULT '', is_default INTEGER NOT NULL DEFAULT 0,
 FOREIGN KEY(user_id) REFERENCES ecs_users(user_id) ON DELETE CASCADE);
CREATE TABLE IF NOT EXISTS ecs_cart (
 rec_id INTEGER PRIMARY KEY AUTOINCREMENT, user_id INTEGER NOT NULL, goods_id INTEGER NOT NULL, goods_number INTEGER NOT NULL CHECK(goods_number>0), version INTEGER NOT NULL DEFAULT 1, UNIQUE(user_id,goods_id), FOREIGN KEY(user_id) REFERENCES ecs_users(user_id) ON DELETE CASCADE, FOREIGN KEY(goods_id) REFERENCES ecs_goods(goods_id));
CREATE TABLE IF NOT EXISTS ecs_shipping (
 shipping_id INTEGER PRIMARY KEY, shipping_name VARCHAR(120) NOT NULL, enabled INTEGER NOT NULL DEFAULT 1, shipping_fee DECIMAL(12,2) NOT NULL DEFAULT 0.00);
CREATE TABLE IF NOT EXISTS ecs_payment (
 pay_id INTEGER PRIMARY KEY, pay_name VARCHAR(120) NOT NULL, enabled INTEGER NOT NULL DEFAULT 1, pay_fee DECIMAL(12,2) NOT NULL DEFAULT 0.00);
CREATE TABLE IF NOT EXISTS ecs_order_info (
 order_id INTEGER PRIMARY KEY AUTOINCREMENT, order_sn VARCHAR(40) NOT NULL UNIQUE, user_id INTEGER NOT NULL, order_status VARCHAR(32) NOT NULL DEFAULT 'pending_payment', consignee VARCHAR(60) NOT NULL, address VARCHAR(255) NOT NULL, mobile VARCHAR(32) NOT NULL DEFAULT '', shipping_id INTEGER NOT NULL, pay_id INTEGER NOT NULL, goods_amount DECIMAL(12,2) NOT NULL, shipping_fee DECIMAL(12,2) NOT NULL DEFAULT 0.00, payment_fee DECIMAL(12,2) NOT NULL DEFAULT 0.00, order_amount DECIMAL(12,2) NOT NULL, idempotency_key VARCHAR(100) NOT NULL, request_fingerprint TEXT NOT NULL, remark VARCHAR(255) NOT NULL DEFAULT '', created_at INTEGER NOT NULL DEFAULT (unixepoch()), UNIQUE(user_id,idempotency_key), FOREIGN KEY(user_id) REFERENCES ecs_users(user_id), FOREIGN KEY(shipping_id) REFERENCES ecs_shipping(shipping_id), FOREIGN KEY(pay_id) REFERENCES ecs_payment(pay_id));
CREATE TABLE IF NOT EXISTS ecs_order_goods (
 rec_id INTEGER PRIMARY KEY AUTOINCREMENT, order_id INTEGER NOT NULL, goods_id INTEGER NOT NULL, goods_name VARCHAR(255) NOT NULL, goods_number INTEGER NOT NULL CHECK(goods_number>0), goods_price DECIMAL(12,2) NOT NULL, FOREIGN KEY(order_id) REFERENCES ecs_order_info(order_id), FOREIGN KEY(goods_id) REFERENCES ecs_goods(goods_id));
CREATE TABLE IF NOT EXISTS ecs_order_action (
 action_id INTEGER PRIMARY KEY AUTOINCREMENT, order_id INTEGER NOT NULL, actor_user_id INTEGER NOT NULL, action VARCHAR(32) NOT NULL, note VARCHAR(255) NOT NULL DEFAULT '', created_at INTEGER NOT NULL DEFAULT (unixepoch()), FOREIGN KEY(order_id) REFERENCES ecs_order_info(order_id), FOREIGN KEY(actor_user_id) REFERENCES ecs_users(user_id));
CREATE TABLE IF NOT EXISTS ecs_comment (
 comment_id INTEGER PRIMARY KEY AUTOINCREMENT, comment_type INTEGER NOT NULL DEFAULT 0, id_value INTEGER NOT NULL, user_id INTEGER NOT NULL, user_name VARCHAR(60) NOT NULL, content TEXT NOT NULL, status INTEGER NOT NULL DEFAULT 1, add_time INTEGER NOT NULL DEFAULT (unixepoch()), FOREIGN KEY(id_value) REFERENCES ecs_goods(goods_id), FOREIGN KEY(user_id) REFERENCES ecs_users(user_id));
CREATE INDEX IF NOT EXISTS idx_ecs_comment_goods_status ON ecs_comment(id_value,status,comment_id DESC);
CREATE TABLE IF NOT EXISTS ecs_pay_log (
 log_id INTEGER PRIMARY KEY AUTOINCREMENT, order_id INTEGER NOT NULL, provider VARCHAR(60) NOT NULL, provider_trade_no VARCHAR(120) NOT NULL, amount DECIMAL(12,2) NOT NULL, status VARCHAR(32) NOT NULL, raw_payload TEXT NOT NULL, received_at INTEGER NOT NULL DEFAULT (unixepoch()), UNIQUE(provider,provider_trade_no), FOREIGN KEY(order_id) REFERENCES ecs_order_info(order_id));
CREATE TABLE IF NOT EXISTS ecs_goods_activity (
 act_id INTEGER PRIMARY KEY, act_name VARCHAR(255) NOT NULL, act_desc TEXT NOT NULL DEFAULT '', act_type INTEGER NOT NULL, goods_id INTEGER NOT NULL, product_id INTEGER NOT NULL DEFAULT 0, goods_name VARCHAR(255) NOT NULL DEFAULT '', start_time INTEGER NOT NULL, end_time INTEGER NOT NULL, is_finished INTEGER NOT NULL DEFAULT 0, ext_info TEXT NOT NULL DEFAULT '', FOREIGN KEY(goods_id) REFERENCES ecs_goods(goods_id));
CREATE INDEX IF NOT EXISTS idx_ecs_activity_active ON ecs_goods_activity(act_type,start_time,end_time,is_finished);
INSERT OR IGNORE INTO ecs_category(cat_id, cat_name) VALUES(1, '示例分类');
INSERT OR IGNORE INTO ecs_brand(brand_id, brand_name) VALUES(1, '示例品牌');
INSERT OR IGNORE INTO ecs_article_cat(cat_id, cat_name, cat_desc) VALUES(1, '商城公告', '示例文章分类');
INSERT OR IGNORE INTO ecs_article(article_id, cat_id, title, author, article_desc, content, is_open) VALUES(1, 1, 'C# ECSHOP 项目说明', 'csharp_ecshop', '用于验证文章 URL 的示例文章', '这是来自 SQLite ecs_article 表的示例正文。', 1);
INSERT OR IGNORE INTO ecs_region(region_id, parent_id, region_name, region_type) VALUES(1, 0, '中国', 1);
INSERT OR IGNORE INTO ecs_region(region_id, parent_id, region_name, region_type) VALUES(2, 1, '北京市', 2);
INSERT OR IGNORE INTO ecs_goods(goods_id,cat_id,goods_sn,goods_name,brand_id,goods_number,market_price,shop_price,goods_brief,goods_desc,is_on_sale,is_delete) VALUES(12,1,'CS-EC-001','C# 入门商品',1,18,69.90,49.90,'用于验证第一个 HTTP URL 的示例商品','来自 SQLite ecs_goods 表',1,0);
INSERT OR IGNORE INTO ecs_goods(goods_id,cat_id,goods_sn,goods_name,brand_id,goods_number,market_price,shop_price,goods_brief,goods_desc,is_on_sale,is_delete) VALUES(13,1,'CPP-EC-002','已下架商品',1,5,129.90,99.90,'不可公开访问','用于验证可见性过滤',0,0);
INSERT OR IGNORE INTO ecs_goods_attr(goods_attr_id,goods_id,attr_id,attr_value,attr_price) VALUES(1001,12,1,'扩展版','5.00');
INSERT OR IGNORE INTO ecs_goods_attr(goods_attr_id,goods_id,attr_id,attr_value,attr_price) VALUES(1005,12,2,'标准包装','0.00');
INSERT OR IGNORE INTO ecs_products(product_id,goods_id,goods_attr,product_sn,product_number) VALUES(81,12,'1001|1005','CPP-EC-001-EXT',10);
INSERT OR IGNORE INTO ecs_shipping(shipping_id,shipping_name,enabled,shipping_fee) VALUES(1,'标准快递',1,'8.00');
INSERT OR IGNORE INTO ecs_payment(pay_id,pay_name,enabled,pay_fee) VALUES(1,'在线支付',1,'0.00');
INSERT OR IGNORE INTO ecs_goods_activity(act_id,act_name,act_desc,act_type,goods_id,goods_name,start_time,end_time,is_finished,ext_info) VALUES(1,'C# 示例团购','用于学习 goods_activity 的公开读取',1,12,'C# 入门商品',0,4102444800,0,'{"cur_price":"39.90"}');
COMMIT;
