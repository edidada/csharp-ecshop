#!/usr/bin/env bash
set -euo pipefail
base_url="${1:-http://127.0.0.1:8080}"
username="auth_test_$(date +%s)"
password='safe-password-123'

printf '%s\n' '1) Register (expected: HTTP 201)'
register_body=$(curl --noproxy '*' --fail-with-body --silent --show-error --request POST "$base_url/api/v1/auth/register" \
  --header 'Content-Type: application/json' \
  --data "{\"username\":\"$username\",\"email\":\"$username@example.test\",\"password\":\"$password\"}")
printf '%s\n' "$register_body" | sed 's/"access_token":"[^"]*"/"access_token":"***"/'

printf '%s\n' '2) Login (expected: HTTP 200)'
login_body=$(curl --noproxy '*' --fail-with-body --silent --show-error --request POST "$base_url/api/v1/auth/login" \
  --header 'Content-Type: application/json' \
  --data "{\"username\":\"$username\",\"password\":\"$password\"}")
printf '%s\n' "$login_body" | sed 's/"access_token":"[^"]*"/"access_token":"***"/'
token=$(printf '%s' "$login_body" | sed -n 's/.*"access_token":"\([^"]*\)".*/\1/p')

printf '%s\n' '3) Read profile (expected: HTTP 200)'
curl --noproxy '*' --fail-with-body --silent --show-error "$base_url/api/v1/me" --header "Authorization: Bearer $token"

printf '%s\n' '4) Logout (expected: HTTP 204)'
curl --noproxy '*' --silent --show-error --output /dev/null --write-out 'HTTP %{http_code}\n' \
  --request POST "$base_url/api/v1/auth/logout" --header "Authorization: Bearer $token"
