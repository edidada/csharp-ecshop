#!/usr/bin/env bash
set -euo pipefail
database_path="${1:-data/ecshop.sqlite3}"
mkdir -p "$(dirname "$database_path")"
sqlite3 "$database_path" < docs/sql/sqlite/001_ecshop_catalog.sql
