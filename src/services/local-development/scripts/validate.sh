#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
RUNNER_DIR="$(cd -- "$SCRIPT_DIR/.." && pwd)"
cd "$RUNNER_DIR"

# shellcheck disable=SC1091
source "$SCRIPT_DIR/_env.sh"
load_local_env

required=(
    APP_PORT
    MYSQL_DATABASE
    MYSQL_APP_USER
    MYSQL_APP_PASSWORD
    MYSQL_ROOT_PASSWORD
    FINANCE_POSTGRES_DB
    FINANCE_POSTGRES_ADMIN_PASSWORD
    FINANCE_READ_PASSWORD
    FINANCE_WRITE_PASSWORD
)
missing=()
for name in "${required[@]}"; do
    if [[ -z "${!name:-}" ]]; then
        missing+=("$name")
    fi
done

if (( ${#missing[@]} > 0 )); then
    echo "Missing required local environment variable names:" >&2
    printf ' - %s\n' "${missing[@]}" >&2
    exit 2
fi

docker compose config --quiet
echo "Compose configuration is valid."

if [[ "${1:-}" == "--config-only" ]]; then
    exit 0
fi

services=(mysql finance-postgres app)
deadline=$((SECONDS + 120))
for service in "${services[@]}"; do
    while true; do
        container_id="$(docker compose ps -q "$service")"
        if [[ -n "$container_id" ]]; then
            health="$(docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' "$container_id")"
            if [[ "$health" == "healthy" ]]; then
                break
            fi
        fi

        if (( SECONDS >= deadline )); then
            echo "Service did not become healthy: $service" >&2
            exit 3
        fi
        sleep 2
    done
done

if ! docker compose exec -T mysql mysqladmin ping --protocol=socket -uroot -p"$MYSQL_ROOT_PASSWORD" --silent >/dev/null; then
    echo "MySQL connection check failed." >&2
    exit 4
fi

if ! docker compose exec -T finance-postgres pg_isready -U postgres -d "$FINANCE_POSTGRES_DB" >/dev/null; then
    echo "Finance PostgreSQL connection check failed." >&2
    exit 4
fi

if [[ "${LOCAL_DEVELOPMENT_DOCKER:-}" == "1" ]]; then
    http_url="http://app:8080/"
    http_host_header="localhost"
else
    http_url="http://127.0.0.1:${APP_PORT}/"
    http_host_header=""
fi

if [[ -n "$http_host_header" ]]; then
    http_check=(curl --fail --silent --header "Host: $http_host_header" "$http_url")
else
    http_check=(curl --fail --silent "$http_url")
fi

if ! "${http_check[@]}" >/dev/null; then
    echo "GamMaSite HTTP check failed." >&2
    exit 5
fi

echo "Local services and database connections are healthy."
