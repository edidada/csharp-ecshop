# URL 与功能清单

PHP 参考项目把页面、表单提交和 AJAX 混在 `xxx.php?act=...` / `flow.php?step=...` 入口中。C++ 版本将页面与 API 分离：浏览器页面后续可由 SPA/SSR 消费 JSON API；下表是实现优先级而非已实现清单。

## 前台核心映射

| 优先级 | PHP 入口（参考） | C++ REST API | 方法 | 主要表 |
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

## 旧入口覆盖范围

参考项目顶层还包括 `activity.php`、`affiche.php`、`api.php`、`captcha.php`、`compare.php`、`exchange.php`、`feed.php`、`gallery.php`、`message.php`、`package.php`、`tag_cloud.php`、`topic.php`、`vote.php`、`wholesale.php` 等。它们归入 M5 内容、营销或兼容模块，不应阻塞主交易链路。

后台 PHP 入口位于 `upload/admin/`，例如 `goods.php`、`order.php`、`users.php`、`category.php`、`payment.php`、`shipping.php`。新项目应另设 `/api/v1/admin/**`，使用角色权限而非沿用 PHP 文件名和 `act` 参数。

## 通用查询参数

`page` 从 1 开始；`page_size` 默认 20、最大 100；列表响应恒含 `page`、`page_size`、`total`、`items`。商品列表支持 `category_id`、`brand_id`、`q`、`sort`（`price_asc|price_desc|newest|sales`）。所有金额用十进制字符串，例如 `"99.90"`，不使用浮点数。
