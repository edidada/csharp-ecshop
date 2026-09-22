# URL 与功能清单

PHP 参考项目把页面、表单提交和 AJAX 混在 `xxx.php?act=...` / `flow.php?step=...` 入口中。C# 版本将页面与 API 分离：浏览器页面后续可由 SPA/SSR 消费 JSON API。下表中的具体 REST 路由均已编码，等待按 [集中测试方案](06_curl_testing.md) 在三种数据库上统一验收；优先级保留用于说明迁移顺序。

## 前台核心映射

| 优先级 | PHP 入口（参考） | C# REST API | 方法 | 主要表 |
| --- | --- | --- | --- |
| M0 | — | `/healthz` | GET | — |
| M1 | `index.php` | `/api/v1/home` | GET | goods, category, article |
| M1 | `category.php?id=` | `/api/v1/categories/{id}/goods` | GET | category, goods, goods_cat |
| M1 | `goods.php?id=` | `/api/v1/goods/{id}` | GET | goods, goods_gallery, goods_attr |
| M1 | `goods.php?act=price` | `/api/v1/goods/{id}/price-quote` | POST | goods, goods_attr, member_price |
| M1 | `search.php?keywords=` | `/api/v1/goods` | GET | goods, keywords |
| M1 | `brand.php` | `/api/v1/brands`、`/api/v1/brands/{id}/goods` | GET | brand, goods |
| M1 | `article.php` / `article_cat.php` | `/api/v1/articles/{id}`、`/api/v1/article-categories/{id}/articles` | GET | article, article_cat |
| M1 | `region.php` | `/api/v1/regions?parent=&type=` | GET | region |
| M2 | `user.php?act=act_register` | `/api/v1/auth/register` | POST | users, reg_fields |
| M2 | `user.php?act=act_login` | `/api/v1/auth/login` | POST | users, sessions |
| M2 | `user.php?act=logout` | `/api/v1/auth/logout` | POST | sessions |
| M2 | `user.php?act=profile` | `/api/v1/me` | GET/PATCH | users |
| M2 | `user.php?act=address_list` | `/api/v1/me/addresses` | GET/POST |
| M2 | `user.php?act=act_edit_address` | `/api/v1/me/addresses/{id}` | PATCH | user_address |
| M2 | `flow.php?step=cart` | `/api/v1/me/cart` | GET | cart, goods |
| M2 | `flow.php?step=add_to_cart` | `/api/v1/me/cart` | POST | cart, goods |
| M2 | `flow.php?step=update_cart` | `/api/v1/me/cart/{recId}` | PATCH | cart |
| M2 | `flow.php?step=drop_goods` | `/api/v1/me/cart/{recId}` | DELETE | cart |
| M2 | `flow.php?step=checkout` | `/api/v1/checkout/options`、`/api/v1/checkout/quote` | GET/POST | cart, user_address, shipping, payment |
| M2 | `flow.php?step=done` | `/api/v1/orders` | POST | order_info, order_goods, goods |
| M2 | `user.php?act=order_list` | `/api/v1/me/orders` | GET | order_info |
| M2 | `user.php?act=order_detail` | `/api/v1/me/orders/{id}` | GET | order_info, order_goods |
| M2 | `user.php?act=cancel_order` | `/api/v1/me/orders/{id}/cancel` | POST | order_info, order_goods, order_action, goods |
| M2 | `respond.php` | `/api/v1/payments/{provider}/callback` | POST | pay_log, order_info, order_action |
| M2 | `comment.php` | `/api/v1/goods/{id}/comments` | GET/POST | comment, goods, users |
| M5 | `group_buy.php` / `auction.php` / `snatch.php` | `/api/v1/promotions/*` | GET/POST | goods_activity, auction_log, snatch_log |
| M5 | `activity.php` | `/api/v1/activities` | GET | goods_activity |
| M5 | `affiche.php` | `/api/v1/announcements` | GET | article |
| M5 | `api.php` | `/api/v1/compat/status` | GET | — |
| M5 | `captcha.php` | `/api/v1/captcha`、`/api/v1/captcha/verify` | GET/POST | memory cache |
| M5 | `compare.php` | `/api/v1/compare?goods_ids=` | GET | goods |
| M5 | `exchange.php` | `/api/v1/exchange-goods` | GET | exchange_goods, goods |
| M5 | `feed.php` | `/api/v1/feed` | GET | goods, article |
| M5 | `gallery.php` | `/api/v1/goods/{id}/gallery` | GET | goods_gallery |
| M5 | `message.php` | `/api/v1/messages` | GET/POST | feedback, users |
| M5 | `package.php` | `/api/v1/packages` | GET | goods_activity, package_goods |
| M5 | `tag_cloud.php` | `/api/v1/tags`、`/api/v1/goods/{id}/tags` | GET/GET/POST | tag, goods, users |
| M5 | `topic.php` | `/api/v1/topics/{id}` | GET | topic |
| M5 | `vote.php` | `/api/v1/votes/{id}` | GET/POST | vote, vote_option, vote_log |
| M5 | `wholesale.php` | `/api/v1/wholesale` | GET | wholesale, goods |

## 旧入口覆盖范围

参考项目上述内容、营销与兼容入口已展开为 M5 JSON API。未映射的支付渠道专用接收页、证书页、站点地图文件等属于部署或渠道适配器输出，不暴露为通用业务 JSON 写接口。

后台 PHP 入口位于 `upload/admin/`，例如 `goods.php`、`order.php`、`users.php`、`category.php`、`payment.php`、`shipping.php`。新项目应另设 `/api/v1/admin/**`，使用角色权限而非沿用 PHP 文件名和 `act` 参数。

## 通用查询参数

支持分页的商品、文章、订单、评论和留言列表中，`page` 从 1 开始，`page_size` 默认 20、最大 100，响应包含 `page`、`page_size`、`total`、`items`。品牌、区域等小型参考数据直接返回数组；聚合入口返回命名集合。商品列表支持 `category_id`、`brand_id`、`q`、`sort`（`price_asc|price_desc|newest|sales`）。所有金额用十进制字符串，例如 `"99.90"`，不使用浮点数。
