# HTTP API 契约

这是审核后的业务 URL 的目标契约；当前仅实现基础设施探针 `/healthz` 和 `/readyz`。Base URL：`http://localhost:8080/api/v1`；仅 JSON，`Content-Type: application/json; charset=utf-8`。成功响应直接返回资源；错误固定为：

```json
{"code":"validation_error","message":"quantity must be between 1 and 999","request_id":"...","details":{"field":"quantity"}}
```

| HTTP | `code` | 使用场景 |
| --- | --- | --- |
| 400 | `validation_error` | JSON、类型、范围或业务前置条件错误 |
| 401 | `unauthenticated` | 无效、过期或缺失会话 |
| 403 | `forbidden` | 已登录但无资源权限 |
| 404 | `not_found` | 商品、订单等不存在或不可见 |
| 409 | `conflict` / `out_of_stock` | 用户重复、版本冲突、库存不足 |
| 422 | `invalid_state` | 订单状态不允许该动作 |
| 500 | `internal_error` | 不泄露 SQL/堆栈；记录 request_id |

鉴权：当前实现使用 `Authorization: Bearer <access_token>`。令牌只在登录/注册响应中明文返回一次，服务端只存 SHA-256 哈希。订单等后续接口会再增加幂等键。

## 商品

`GET /goods/{id}` 返回商品、图、规格、库存摘要；不存在为 404。

`POST /goods/{id}/price-quote`

```json
{"quantity":2,"product_id":81,"attribute_ids":[1001,1005]}
```

```json
{"goods_id":12,"quantity":2,"unit_price":"49.90","total":"99.80","currency":"CNY","stock_available":18}
```

该接口对应 PHP `goods.php?act=price`。价格只用于展示；下单时必须重新计算，客户端总价不可被信任。

## 账号与地址

`POST /auth/register`

```json
{"username":"alice","email":"alice@example.test","password":"not-a-real-password","agreement_accepted":true}
```

返回 `201` 和 `{ "id": 7, "username": "alice" }`。密码长度、唯一性和邮箱格式由服务端验证，数据库仅存现代、可升级的密码哈希。

`POST /auth/login` 请求 `{ "username":"alice", "password":"..." }`，成功返回 204 并写入 Cookie。`POST /auth/logout` 返回 204 并作废当前会话。

`POST /me/addresses` 请求：

```json
{"consignee":"张三","country_id":1,"province_id":2,"city_id":52,"district_id":500,"address":"中山路 1 号","mobile":"13800000000","zip":"200000","is_default":true}
```

返回 201。`country_id` 到 `district_id` 必须存在于 `region`，并属于合法层级。

## 购物车

所有购物车接口均要求 Bearer token，且用户 ID 仅从该 token 对应的 session 取得。

`POST /me/cart`：

```json
{"goods_id":12,"quantity":2}
```

返回 201 的购物车行：

```json
{"id":301,"goods_id":12,"name":"示例商品","price":"49.90","quantity":2,"version":1}
```

`GET /me/cart` 返回 `{ "items": [...], "total_quantity": 2 }`。更新 `PATCH /me/cart/{id}` 请求 `{ "quantity": 3, "version": 1 }`。`version` 是乐观锁版本：SQL 的 `WHERE` 同时匹配 `id`、当前用户与 `version`，失配返回 409，客户端重新 GET `/me/cart`。删除为 `DELETE /me/cart/{id}`，成功 204。

当前购物车只实现无 SKU/属性组合的基础商品行；同一用户同一商品的 POST 会原子累加数量并递增版本。SQLite 使用 `UNIQUE(user_id, goods_id)` + upsert，MySQL 使用对应唯一键 + `ON DUPLICATE KEY UPDATE`，两者都在事务中取得行 ID。

## 结算与订单

`POST /checkout/quote`：

```json
{"address_id":9,"shipping_id":1,"payment_id":1}
```

先用 `GET /checkout/options` 读取可用 `shipping` 与 `payment`。报价验证地址属于当前 Bearer 用户、配送/支付方式启用且购物车有可售商品；响应以当前商品价格计算：

```json
{"address_id":9,"shipping_id":1,"payment_id":1,"items":[...],"goods_amount":"99.80","shipping_fee":"8.00","payment_fee":"0.00","order_amount":"107.80"}
```

金额以分的整数在服务端计算，JSON 中以两位小数的字符串输出。订单创建尚会再次以数据库当前数据重算，不信任客户端报价。

`POST /orders` 必带 `Idempotency-Key`：

```json
{"address_id":9,"shipping_id":1,"payment_id":1,"remark":"工作日送达"}
```

返回 201：

```json
{"id":9001,"order_sn":"EC...","status":"pending_payment","goods_amount":"99.80","shipping_fee":"8.00","payment_fee":"0.00","order_amount":"107.80","replayed":false}
```

服务端在同一个数据库事务内重新读取购物车、条件式扣减库存、写入订单与订单商品快照并清空购物车。库存不足时全部回滚并返回 409。相同用户、相同幂等键和相同请求重放同一结果（200 且 `replayed:true`）；同键不同内容返回 409。支付回调只接受支付渠道签名；不得用浏览器重定向作为支付成功依据。

`GET /me/orders` 返回当前用户的订单摘要列表。`GET /me/orders/{id}` 返回订单收货信息和 `order_goods` 快照；两个查询都以已认证用户 ID 作为 SQL 条件，其他用户的订单与不存在订单一律返回 404。

`POST /me/orders/{id}/cancel` 仅能取消当前用户仍为 `pending_payment` 的订单。成功后返回状态为 `cancelled` 的订单摘要；同一事务会回补 `order_goods` 数量到库存并插入订单操作审计。已取消、已付款或其他非待支付状态返回 409，因状态条件更新失败而不会重复回补库存。

## 商品评论

`GET /goods/{id}/comments` 返回已发布评论列表。`POST /goods/{id}/comments` 要求 Bearer token：

```json
{"content":"商品符合预期"}
```

内容长度为 1--2000 个字节；服务端从 session 获取评论用户名并使用绑定参数写库。当前开发配置自动发布评论（`status=1`）；生产审核模式可改为先写未发布状态。下架或不存在商品返回 404。

## 支付回调

`POST /payments/{provider}/callback` 不使用用户 Bearer token。渠道适配器必须提交原始 JSON body，并在 `X-Payment-Signature` 携带该原始 body 的 HMAC-SHA256 十六进制签名。当前统一 payload：

```json
{"provider_trade_no":"gateway-transaction-1","order_sn":"EC...","amount":"107.80"}
```

服务器先验签，再检查金额与当前订单应付金额相等，最后在一个事务中将 `pending_payment` 订单变为 `paid`、插入 `pay_log` 和审计行。`(provider, provider_trade_no)` 是唯一键：同一交易、相同金额/订单号重放返回 200，冲突交易号返回 409。生产配置必须用环境变量提供 `payment.callback_hmac_secret`，不得使用开发 secret。
