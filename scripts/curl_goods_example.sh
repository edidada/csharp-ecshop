#!/usr/bin/env bash
set -euo pipefail
base_url="${1:-http://127.0.0.1:8080}"
printf '%s\n' '1) Health check'
curl --noproxy '*' --fail-with-body --silent --show-error "$base_url/healthz"
printf '\n\n%s\n' '2) Existing sellable goods (expected: HTTP 200)'
curl --noproxy '*' --fail-with-body --silent --show-error "$base_url/api/v1/goods/12"
printf '\n\n%s\n' '3) Unavailable goods (expected: HTTP 404)'
curl --noproxy '*' --silent --show-error --write-out '\nHTTP %{http_code}\n' "$base_url/api/v1/goods/13"

printf '\n%s\n' '4) Search goods (expected: HTTP 200)'
curl --noproxy '*' --fail-with-body --silent --show-error \
  "$base_url/api/v1/goods?q=C%23&page=1&page_size=5"

printf '\n\n%s\n' '5) Quote a goods price (expected: HTTP 200)'
curl --noproxy '*' --fail-with-body --silent --show-error \
  --request POST "$base_url/api/v1/goods/12/price-quote" \
  --header 'Content-Type: application/json' \
  --data '{"quantity":2}'

printf '\n\n%s\n' '6) List brands (expected: HTTP 200)'
curl --noproxy '*' --fail-with-body --silent --show-error "$base_url/api/v1/brands"

printf '\n\n%s\n' '7) List brand goods (expected: HTTP 200)'
curl --noproxy '*' --fail-with-body --silent --show-error \
  "$base_url/api/v1/brands/1/goods?page=1&page_size=5"

printf '\n\n%s\n' '8) Read an article (expected: HTTP 200)'
curl --noproxy '*' --fail-with-body --silent --show-error "$base_url/api/v1/articles/1"

printf '\n\n%s\n' '9) List category articles (expected: HTTP 200)'
curl --noproxy '*' --fail-with-body --silent --show-error \
  "$base_url/api/v1/article-categories/1/articles?page=1&page_size=5"

printf '\n\n%s\n' '10) List regions (expected: HTTP 200)'
curl --noproxy '*' --fail-with-body --silent --show-error "$base_url/api/v1/regions?parent=0&type=1"
