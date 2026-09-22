# HTTP API 契约

Base URL 为 `http://localhost:8080/api/v1`。请求 JSON 使用 camelCase，响应使用 snake_case；金额响应均为两位小数字符串。受保护接口使用 `Authorization: Bearer <access_token>`，服务端只保存 token 的 SHA-256 哈希。

## 公开目录

- `GET /home`
- `GET /categories/{id}/goods?page=&page_size=`
- `GET /goods/{id}`
- `POST /goods/{id}/price-quote`：`{"quantity":2,"productId":81,"attributeIds":[1001,1005]}`
- `GET /goods?category_id=&brand_id=&q=&sort=&page=&page_size=`
- `GET /brands`、`GET /brands/{id}/goods`
- `GET /articles/{id}`、`GET /article-categories/{id}/articles`
- `GET /regions?parent=&type=`

商品只公开 `is_on_sale=true` 且 `is_delete=false` 的记录。价格报价只用于展示，下单时重新读取数据库价格和库存。

## 身份、资料与地址

注册请求：

~~~json
{"username":"alice","email":"alice@example.test","password":"not-a-real-password","agreementAccepted":true}
~~~

`POST /auth/register` 返回 201 和一次性明文 `access_token`；`POST /auth/login` 返回 200 和新 token；`POST /auth/logout` 返回 204 并作废当前 token。`GET /me` 读取资料，`PATCH /me` 接受可选 `email`。

地址路由为 `GET/POST /me/addresses`、`PATCH /me/addresses/{id}`。请求字段为 `consignee`、`countryId`、`provinceId`、`cityId`、`districtId`、`address`、`mobile`、`zipcode`、`isDefault`。地址查询和修改始终以 token 所属用户过滤。

## 购物车与结算

`POST /me/cart` 请求 `{"goodsId":12,"quantity":2}`；`PATCH /me/cart/{id}` 请求 `{"quantity":3,"version":1}`；删除成功返回 204。版本不一致返回 409，同一用户同一商品只保留一行。

`GET /checkout/options` 返回可用配送和支付方式。`POST /checkout/quote` 请求：

~~~json
{"addressId":9,"shippingId":1,"paymentId":1}
~~~

报价验证地址归属、购物车、商品可见性、配送和支付状态，并返回当前商品金额、费用和应付金额。

## 订单

`POST /orders` 请求：

~~~json
{"addressId":9,"shippingId":1,"paymentId":1,"idempotencyKey":"client-request-001"}
~~~

首次成功返回 201；同一用户重放相同 `idempotencyKey` 返回 200 和已有订单。订单事务重新读取购物车和库存、保存商品快照、扣减库存并清空购物车。

`GET /me/orders?page=&page_size=` 返回当前用户订单；`GET /me/orders/{id}` 返回摘要及商品快照；`POST /me/orders/{id}/cancel` 只允许取消未支付的新订单，并在事务中回补库存。状态值当前采用 ECSHOP 数值：新订单 0、用户取消 2；未支付 0、已支付 2。

## 评论、支付与营销

`GET /goods/{id}/comments` 返回可见评论；认证用户用 `POST /goods/{id}/comments` 提交 `{"content":"商品符合预期"}`，内容长度为 1 到 500 个字符。

开发用支付适配器为 `POST /payments/mock/callback`，请求 `{"orderSn":"CS...","transactionId":"gateway-transaction-1"}`。`(provider, transactionId)` 唯一；重复回调返回 `duplicate=true`。其他 provider 返回 404。真实支付渠道必须另行实现签名校验适配器，不能直接启用 mock 契约。

营销路由展开为 `GET/POST /promotions/{kind}`，kind 只允许 `group-buy`、`auction`、`snatch`。创建请求为 `{"name":"活动名","goodsId":12,"startTime":1700000000,"endTime":1700003600}`，写操作要求 Bearer token。

## 旧前台内容与互动入口

- `GET /activities`、`GET /announcements`、`GET /compat/status` 分别对应活动页、公告输出和旧 API 能力探测。
- `GET /captcha` 创建五分钟有效的一次性算术挑战；`POST /captcha/verify` 请求 `{"challengeId":"...","answer":7}`，无论成功失败均消费挑战。
- `GET /compare?goods_ids=12,14` 比较最多五件可见商品；任一商品下架或不存在时返回 404。
- `GET /exchange-goods`、`GET /packages`、`GET /wholesale` 返回积分兑换、礼包和批发配置及其可见商品。
- `GET /feed` 聚合最新商品与文章；`GET /goods/{id}/gallery` 返回商品相册。
- `GET /messages` 返回已发布留言；认证用户用 `POST /messages` 提交 `title`、`content`、`type`、`orderId`。
- `GET /tags` 返回聚合标签云；`GET /goods/{id}/tags` 返回商品标签；认证用户可用 `POST /goods/{id}/tags` 提交逗号分隔的 `tag`。
- `GET /topics/{id}` 返回有效期内专题；`GET /votes/{id}` 返回投票和选项；认证用户用 `POST /votes/{id}` 提交 `{"optionIds":[1]}`，每个用户每个投票只能提交一次。

## 错误与分页

业务校验使用 Problem Details（400）；未认证为 401；资源不存在或不归属为 404；版本、库存和非法状态冲突为 409。列表的 `page` 从 1 开始，`page_size` 默认 20、范围 1 到 100。详细负向测试矩阵见 [06_curl_testing.md](06_curl_testing.md)。
