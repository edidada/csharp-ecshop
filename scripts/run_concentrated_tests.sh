#!/usr/bin/env bash
set -euo pipefail

# One entry point for the later concentrated verification phase.
# By default it runs the deterministic in-process SQLite suite only.
# Set RUN_HTTP_SMOKE=1 after starting one provider-specific API instance.
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

dotnet test tests/EcShop.Api.Tests/EcShop.Api.Tests.csproj --no-restore --logger 'console;verbosity=minimal'

if [[ "${RUN_HTTP_SMOKE:-0}" == "1" ]]; then
  BASE_URL="${BASE_URL:-http://127.0.0.1:8080}" bash scripts/curl_all_urls.sh
else
  echo 'HTTP smoke skipped; set RUN_HTTP_SMOKE=1 after starting the selected database/API instance.'
fi
