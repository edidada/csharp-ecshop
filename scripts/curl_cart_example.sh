#!/usr/bin/env bash
set -euo pipefail

base_url="${1:-http://127.0.0.1:8080}"
username="cart_test_$(date +%s)"
password='safe-cart-password-123'

register=$(curl --noproxy '*' --fail-with-body --silent --show-error --request POST "$base_url/api/v1/auth/register" \
  --header 'Content-Type: application/json' \
  --data "{\"username\":\"$username\",\"email\":\"$username@example.test\",\"password\":\"$password\"}")
token=$(printf '%s' "$register" | sed -n 's/.*"access_token":"\([^"]*\)".*/\1/p')
test -n "$token"
auth_header="Authorization: Bearer $token"

printf '%s\n' '1) Add cart item (expected: HTTP 201)'
item=$(curl --noproxy '*' --fail-with-body --silent --show-error --request POST "$base_url/api/v1/me/cart" \
  --header "$auth_header" --header 'Content-Type: application/json' --data '{"goods_id":12,"quantity":2}')
item_id=$(printf '%s' "$item" | sed -n 's/.*"id":\([0-9][0-9]*\).*/\1/p')
version=$(printf '%s' "$item" | sed -n 's/.*"version":\([0-9][0-9]*\).*/\1/p')
test -n "$item_id" && test -n "$version"
printf '%s\n' "$item"

printf '%s\n' '2) Read cart (expected: HTTP 200)'
curl --noproxy '*' --fail-with-body --silent --show-error "$base_url/api/v1/me/cart" --header "$auth_header"
printf '\n'

printf '%s\n' '3) Update using current version (expected: HTTP 200)'
curl --noproxy '*' --fail-with-body --silent --show-error --request PATCH "$base_url/api/v1/me/cart/$item_id" \
  --header "$auth_header" --header 'Content-Type: application/json' --data "{\"quantity\":3,\"version\":$version}"
printf '\n'

printf '%s\n' '4) Repeat with stale version (expected: HTTP 409)'
stale_status=$(curl --noproxy '*' --silent --show-error --output /dev/null --write-out '%{http_code}' --request PATCH "$base_url/api/v1/me/cart/$item_id" \
  --header "$auth_header" --header 'Content-Type: application/json' --data "{\"quantity\":4,\"version\":$version}")
test "$stale_status" = 409
printf 'HTTP %s\n' "$stale_status"

printf '%s\n' '5) Delete cart item (expected: HTTP 204)'
curl --noproxy '*' --silent --show-error --output /dev/null --write-out 'HTTP %{http_code}\n' --request DELETE "$base_url/api/v1/me/cart/$item_id" --header "$auth_header"
