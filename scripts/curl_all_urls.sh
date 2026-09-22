#!/usr/bin/env bash
set -euo pipefail

# Full positive-path smoke plan for docs/02_url_api_list.md. Start the API first.
base_url="${BASE_URL:-http://127.0.0.1:8080}"
curl_args=(--noproxy '*' --silent --show-error)
suffix="$(date +%s)-$$"
username="curl${suffix}"
email="${username}@example.test"
password='correct-horse-battery-staple'

require() { command -v "$1" >/dev/null || { echo "missing: $1" >&2; exit 127; }; }
require curl; require jq

request() {
  local expected="$1"; shift
  local response status body
  response="$(curl "${curl_args[@]}" --write-out $'\n%{http_code}' "$@")"
  status="${response##*$'\n'}"; body="${response%$'\n'*}"
  [[ "$status" == "$expected" ]] || { echo "expected $expected, got $status: $body" >&2; exit 1; }
  printf '%s' "$body"
}
json() {
  local expected="$1" method="$2" url="$3" payload="${4:-}"
  if [[ -n "$payload" ]]; then request "$expected" -X "$method" "$url" -H 'Content-Type: application/json' -d "$payload"; else request "$expected" -X "$method" "$url" -H 'Content-Type: application/json'; fi
}
auth_json() {
  local expected="$1" method="$2" url="$3" payload="${4:-}"
  if [[ -n "$payload" ]]; then request "$expected" -X "$method" "$url" -H 'Content-Type: application/json' -H "Authorization: Bearer $token" -d "$payload"; else request "$expected" -X "$method" "$url" -H 'Content-Type: application/json' -H "Authorization: Bearer $token"; fi
}
check() { jq -e "$1" >/dev/null; }

echo '[health]'
request 200 "$base_url/healthz" >/dev/null; request 200 "$base_url/readyz" >/dev/null
echo '[catalog]'
request 200 "$base_url/api/v1/home" | check '.goods | length > 0'
request 200 "$base_url/api/v1/categories/1/goods?page=1&page_size=20" | check '.page == 1'
request 200 "$base_url/api/v1/goods/12" | check '.id == 12'
json 200 POST "$base_url/api/v1/goods/12/price-quote" '{"quantity":2,"productId":81,"attributeIds":[1001,1005]}' | check '.total == "109.80"'
request 200 "$base_url/api/v1/goods?q=C%23&sort=price_asc" | check '.items | length > 0'
request 200 "$base_url/api/v1/brands" | check 'length > 0'
request 200 "$base_url/api/v1/brands/1/goods" | check '.items | length > 0'
request 200 "$base_url/api/v1/articles/1" | check '.id == 1'
request 200 "$base_url/api/v1/article-categories/1/articles" | check '.items | length > 0'
request 200 "$base_url/api/v1/regions?parent=0&type=1" | check 'length > 0'
request 200 "$base_url/api/v1/activities" | check '.items | length > 0'
request 200 "$base_url/api/v1/announcements" | check '.items | length > 0'
request 200 "$base_url/api/v1/compat/status" | check '.version == "v1"'
captcha="$(request 200 "$base_url/api/v1/captcha")"
challenge_id="$(printf %s "$captcha" | jq -er '.challenge_id')"; question="$(printf %s "$captcha" | jq -er '.question')"
read -r captcha_left _ captcha_right <<< "$question"
json 200 POST "$base_url/api/v1/captcha/verify" "{\"challengeId\":\"$challenge_id\",\"answer\":$((captcha_left + captcha_right))}" | check '.valid == true'
request 200 "$base_url/api/v1/compare?goods_ids=12" | check '.items[0].id == 12'
request 200 "$base_url/api/v1/exchange-goods" | check '.items | length > 0'
request 200 "$base_url/api/v1/feed" | check '.goods | length > 0'
request 200 "$base_url/api/v1/goods/12/gallery" | check '.items | length > 0'
request 200 "$base_url/api/v1/packages" | check '.items | length > 0'
request 200 "$base_url/api/v1/tags" | check '.items | length > 0'
request 200 "$base_url/api/v1/goods/12/tags" | check '.items | length > 0'
request 200 "$base_url/api/v1/topics/1" | check '.id == 1'
request 200 "$base_url/api/v1/votes/1" | check '.options | length > 0'
request 200 "$base_url/api/v1/wholesale" | check '.items | length > 0'

echo '[identity, address, comment and cart]'
register="$(json 201 POST "$base_url/api/v1/auth/register" "{\"username\":\"$username\",\"email\":\"$email\",\"password\":\"$password\",\"agreementAccepted\":true}")"
token="$(printf %s "$register" | jq -er '.access_token')"
auth_json 200 GET "$base_url/api/v1/me" | check ".username == \"$username\""
auth_json 200 PATCH "$base_url/api/v1/me" "{\"email\":\"updated-$email\"}" | check '.email | startswith("updated-")'
address="$(auth_json 201 POST "$base_url/api/v1/me/addresses" '{"consignee":"curl","countryId":1,"provinceId":2,"cityId":0,"districtId":0,"address":"Road 1","mobile":"13800000000","zipcode":"200000","isDefault":true}')"
address_id="$(printf %s "$address" | jq -er '.id')"
auth_json 200 PATCH "$base_url/api/v1/me/addresses/$address_id" '{"consignee":"updated","countryId":1,"provinceId":2,"cityId":0,"districtId":0,"address":"Road 2","mobile":"13800000000","zipcode":"200001","isDefault":true}' | check '.consignee == "updated"'
auth_json 200 GET "$base_url/api/v1/me/addresses" | check 'length > 0'
auth_json 201 POST "$base_url/api/v1/messages" '{"title":"curl message","content":"all URL smoke message","type":0,"orderId":0}' | check '.id > 0'
request 200 "$base_url/api/v1/messages" | check '.items | length > 0'
auth_json 200 POST "$base_url/api/v1/goods/12/tags" "{\"tag\":\"curl-$suffix\"}" | check '.items | length == 1'
auth_json 200 POST "$base_url/api/v1/votes/1" '{"optionIds":[1]}' | check '.accepted == true'
auth_json 201 POST "$base_url/api/v1/goods/12/comments" '{"content":"curl all URL smoke"}' | check '.id > 0'
request 200 "$base_url/api/v1/goods/12/comments?page=1&page_size=20" | check '.items | length > 0'
cart="$(auth_json 201 POST "$base_url/api/v1/me/cart" '{"goodsId":12,"quantity":2}')"
cart_id="$(printf %s "$cart" | jq -er '.id')"; version="$(printf %s "$cart" | jq -er '.version')"
auth_json 200 PATCH "$base_url/api/v1/me/cart/$cart_id" "{\"quantity\":2,\"version\":$version}" | check '.quantity == 2'
auth_json 200 GET "$base_url/api/v1/me/cart" | check '.total_quantity == 2'

echo '[checkout and cancellable order]'
auth_json 200 GET "$base_url/api/v1/checkout/options" | check '.shipping | length > 0'
auth_json 200 POST "$base_url/api/v1/checkout/quote" "{\"addressId\":$address_id,\"shippingId\":1,\"paymentId\":1}" | check '.order_amount != null'
key="curl-cancel-$suffix"
order="$(auth_json 201 POST "$base_url/api/v1/orders" "{\"addressId\":$address_id,\"shippingId\":1,\"paymentId\":1,\"idempotencyKey\":\"$key\"}")"
order_id="$(printf %s "$order" | jq -er '.id')"
auth_json 200 POST "$base_url/api/v1/orders" "{\"addressId\":$address_id,\"shippingId\":1,\"paymentId\":1,\"idempotencyKey\":\"$key\"}" | check ".id == $order_id"
auth_json 200 GET "$base_url/api/v1/me/orders?page=1&page_size=20" | check ".items | map(.id) | index($order_id) != null"
auth_json 200 GET "$base_url/api/v1/me/orders/$order_id" | check '.order.id > 0'
auth_json 200 POST "$base_url/api/v1/me/orders/$order_id/cancel" | check '.order_status == 2'

echo '[cart delete, payment and promotions]'
temp_cart="$(auth_json 201 POST "$base_url/api/v1/me/cart" '{"goodsId":12,"quantity":1}')"
auth_json 204 DELETE "$base_url/api/v1/me/cart/$(printf %s "$temp_cart" | jq -er '.id')" >/dev/null
auth_json 201 POST "$base_url/api/v1/me/cart" '{"goodsId":12,"quantity":1}' >/dev/null
paid="$(auth_json 201 POST "$base_url/api/v1/orders" "{\"addressId\":$address_id,\"shippingId\":1,\"paymentId\":1,\"idempotencyKey\":\"curl-paid-$suffix\"}")"
order_sn="$(printf %s "$paid" | jq -er '.order_sn')"; transaction_id="curl-tx-$suffix"
json 200 POST "$base_url/api/v1/payments/mock/callback" "{\"orderSn\":\"$order_sn\",\"transactionId\":\"$transaction_id\"}" | check '.accepted == true and .duplicate == false'
json 200 POST "$base_url/api/v1/payments/mock/callback" "{\"orderSn\":\"$order_sn\",\"transactionId\":\"$transaction_id\"}" | check '.accepted == true and .duplicate == true'
now="$(date +%s)"
for kind in group-buy auction snatch; do
  auth_json 201 POST "$base_url/api/v1/promotions/$kind" "{\"name\":\"curl $kind\",\"goodsId\":12,\"startTime\":$((now - 60)),\"endTime\":$((now + 3600))}" | check '.id > 0'
  request 200 "$base_url/api/v1/promotions/$kind" | check '.items | length > 0'
done

echo '[login/logout]'
login="$(json 200 POST "$base_url/api/v1/auth/login" "{\"username\":\"$username\",\"password\":\"$password\"}")"
token="$(printf %s "$login" | jq -er '.access_token')"
auth_json 204 POST "$base_url/api/v1/auth/logout" >/dev/null
request 401 -H "Authorization: Bearer $token" "$base_url/api/v1/me" >/dev/null
echo 'all documented URL smoke checks passed'
