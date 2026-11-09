#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
SERVICE_ROOT="$(cd -- "$SCRIPT_DIR/.." && pwd)"
REPO_ROOT="$(cd -- "$SERVICE_ROOT/../../.." && pwd)"
cd "$SERVICE_ROOT"

# shellcheck disable=SC1091
source "$SCRIPT_DIR/_env.sh"
load_local_env

docker compose up -d mysql finance-postgres

if [[ -x "$REPO_ROOT/src/services/database/.venv/bin/python" ]]; then
    PYTHON="$REPO_ROOT/src/services/database/.venv/bin/python"
elif [[ -x "$REPO_ROOT/src/services/database/.venv/Scripts/python.exe" ]]; then
    PYTHON="$REPO_ROOT/src/services/database/.venv/Scripts/python.exe"
else
    echo "Database service virtual environment is missing. Run: python -m venv src/services/database/.venv" >&2
    exit 2
fi

# The documented commands are launched from this service directory, but the
# Python package lives at the repository root. Make that import root explicit.
cd "$REPO_ROOT"

export DBMANAGER_MYSQL_LOCAL_CONNECTION_STRING="Server=127.0.0.1;Port=${MYSQL_PORT:-3306};Database=${MYSQL_DATABASE:-gammasite_local};User=${MYSQL_APP_USER:-gammasite_app};Password=${MYSQL_APP_PASSWORD:-change-me-local};"
export DBMANAGER_POSTGRES_LOCAL_CONNECTION_STRING="Host=127.0.0.1;Port=${FINANCE_POSTGRES_PORT:-5433};Database=${FINANCE_POSTGRES_DB:-gamma_finance_local};Username=gamma_finance_write;Password=${FINANCE_WRITE_PASSWORD:-change-me-local-finance-write};"

"$PYTHON" -m src.services.database validate \
    --db mysql \
    --env local \
    --connection-env DBMANAGER_MYSQL_LOCAL_CONNECTION_STRING

"$PYTHON" -m src.services.database validate \
    --db postgres \
    --env local \
    --connection-env DBMANAGER_POSTGRES_LOCAL_CONNECTION_STRING
