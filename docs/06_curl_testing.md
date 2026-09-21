# curl HTTP 接口测试

下列命令是当前已实现 URL 的验收示例。开发服务器假设监听 `127.0.0.1:8080`，SQLite 初始化数据存在商品 `12`。全部 curl 显式禁用代理，避免本机代理配置干扰 localhost 测试。

```bash
export BASE_URL=http://127.0.0.1:8080
export COOKIE_JAR="$(mktemp)"
curl --noproxy '*' -fsS "$BASE_URL/healthz" | jq .
curl --noproxy '*' -fsS "$BASE_URL/api/v1/goods/12" | jq .
curl --noproxy '*' -fsS "$BASE_URL/api/v1/goods?category_id=1&page=1&page_size=20" | jq .
```

## 账号与购物车

```bash
REGISTER=$(curl --noproxy '*' -fsS -X POST "$BASE_URL/api/v1/auth/register" \
  -H 'Content-Type: application/json' \
  -d '{"username":"curl-user","email":"curl-user@example.test","password":"correct-horse-battery-staple"}')
TOKEN=$(printf '%s' "$REGISTER" | jq -r .access_token)

AUTH=(-H "Authorization: Bearer $TOKEN")

ADD=$(curl --noproxy '*' -fsS -X POST "$BASE_URL/api/v1/me/cart" "${AUTH[@]}" \
  -H 'Content-Type: application/json' -d '{"goods_id":12,"quantity":2}')
printf '%s\n' "$ADD" | jq .
ITEM_ID=$(printf '%s' "$ADD" | jq -r .id)
VERSION=$(printf '%s' "$ADD" | jq -r .version)

curl --noproxy '*' -fsS "$BASE_URL/api/v1/me/cart" "${AUTH[@]}" | jq .
curl --noproxy '*' -i -fsS -X PATCH "$BASE_URL/api/v1/me/cart/$ITEM_ID" "${AUTH[@]}" \
  -H 'Content-Type: application/json' -d "{\"quantity\":3,\"version\":$VERSION}"
curl --noproxy '*' -i -sS -X DELETE "$BASE_URL/api/v1/me/cart/$ITEM_ID" "${AUTH[@]}"
```

可直接执行完整、无代理的购物车验收：`bash scripts/curl_cart_example.sh`。脚本创建一个带时间戳的本地测试用户，不显示 access token，并验证旧版本 PATCH 的 HTTP 409。

## 地址与报价

```bash
ADDRESS=$(curl --noproxy '*' -fsS -X POST "$BASE_URL/api/v1/me/addresses" "${AUTH[@]}" \
  -H 'Content-Type: application/json' \
  -d '{"consignee":"测试用户","country_id":1,"province_id":2,"city_id":52,"district_id":500,"address":"中山路 1 号","mobile":"13800000000","zipcode":"200000","is_default":true}')
ADDRESS_ID=$(printf '%s' "$ADDRESS" | jq -r .id)

curl --noproxy '*' -fsS "$BASE_URL/api/v1/checkout/options" "${AUTH[@]}" | jq .
QUOTE=$(curl --noproxy '*' -fsS -X POST "$BASE_URL/api/v1/checkout/quote" "${AUTH[@]}" \
  -H 'Content-Type: application/json' \
  -d "{\"address_id\":$ADDRESS_ID,\"shipping_id\":1,\"payment_id\":1}")
printf '%s\n' "$QUOTE" | jq .
```

## 创建订单

```bash
ORDER_BODY="{\"address_id\":$ADDRESS_ID,\"shipping_id\":1,\"payment_id\":1,\"remark\":\"curl test\"}"
ORDER=$(curl --noproxy '*' -fsS -X POST "$BASE_URL/api/v1/orders" "${AUTH[@]}" \
  -H 'Content-Type: application/json' -H 'Idempotency-Key: curl-order-001' \
  -d "$ORDER_BODY")
printf '%s\n' "$ORDER" | jq .

# 重放同一个请求：HTTP 200，返回相同订单和 replayed:true
curl --noproxy '*' -fsS -X POST "$BASE_URL/api/v1/orders" "${AUTH[@]}" \
  -H 'Content-Type: application/json' -H 'Idempotency-Key: curl-order-001' \
  -d "$ORDER_BODY" | jq .

curl --noproxy '*' -fsS "$BASE_URL/api/v1/me/orders" "${AUTH[@]}" | jq .
ORDER_ID=$(printf '%s' "$ORDER" | jq -r .id)
curl --noproxy '*' -fsS "$BASE_URL/api/v1/me/orders/$ORDER_ID" "${AUTH[@]}" | jq .

# 仅 pending_payment 订单可取消；成功后库存回补。
curl --noproxy '*' -fsS -X POST "$BASE_URL/api/v1/me/orders/$ORDER_ID/cancel" "${AUTH[@]}" | jq .
```

将 `quantity` 改成 0 应得到 400 + `validation_error`；地址 ID 换成其他用户的地址或不存在的 ID，应得到 404。同一幂等键传入不同 `remark` 应得到 409；订单成功后 GET `/me/cart` 应为空。重复发送取消订单请求应得到 409，且库存只回补一次。

## 商品评论

```bash
curl --noproxy '*' -fsS -X POST "$BASE_URL/api/v1/goods/12/comments" "${AUTH[@]}" \
  -H 'Content-Type: application/json' -d '{"content":"curl comment"}' | jq .
curl --noproxy '*' -fsS "$BASE_URL/api/v1/goods/12/comments" | jq .
```

不带 Bearer token 发表应得到 401；向下架或不存在商品发表/读取应得到 404。

## 支付回调

开发环境的回调 secret 来自 `config/app.dev.conf`；生产请改为环境变量。订单创建成功后：

```bash
CALLBACK_BODY="{\"provider_trade_no\":\"curl-trade-001\",\"order_sn\":\"$(printf '%s' \"$ORDER\" | jq -r .order_sn)\",\"amount\":\"$(printf '%s' \"$ORDER\" | jq -r .order_amount)\"}"
SIGNATURE=$(printf '%s' "$CALLBACK_BODY" | openssl dgst -sha256 -hmac "$PAYMENT_CALLBACK_SECRET" -hex | sed 's/^.* //')
curl --noproxy '*' -fsS -X POST "$BASE_URL/api/v1/payments/mockpay/callback" \
  -H 'Content-Type: application/json' -H "X-Payment-Signature: $SIGNATURE" \
  -d "$CALLBACK_BODY" | jq .
```

同一 body 和签名重发应返回 `replayed:true`。错误签名应得到 401；金额不同应得到 409。

测试结束后删除临时 cookie：`rm -f "$COOKIE_JAR"`。
