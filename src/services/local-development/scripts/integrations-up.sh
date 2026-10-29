#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
RUNNER_DIR="$(cd -- "$SCRIPT_DIR/.." && pwd)"
cd "$RUNNER_DIR"

# shellcheck disable=SC1091
source "$SCRIPT_DIR/_env.sh"
load_local_env

if [[ -z "${STRIPE_SECRET_API_KEY:-}" ]]; then
    echo "STRIPE_SECRET_API_KEY is required for the Docker-based Stripe CLI." >&2
    exit 2
fi

sanitize_logs() {
    local service="$1"
    docker compose --profile integrations logs --no-color --tail=100 "$service" 2>&1 \
        | sed -E \
            -e 's/(sk|rk)_(test|live)_[A-Za-z0-9._-]+/[REDACTED_API_KEY]/g' \
            -e 's/whsec_[A-Za-z0-9_-]+/[REDACTED_WEBHOOK_SECRET]/g' \
        | tail -n 40
}

wait_for_value() {
    local service="$1"
    local pattern="$2"
    local label="$3"
    local value=""
    local logs=""
    local deadline=$((SECONDS + 60))

    while [[ "$SECONDS" -lt "$deadline" ]]; do
        logs="$(docker compose --profile integrations logs --no-color --tail=100 "$service" 2>&1 || true)"
        value="$(printf '%s\n' "$logs" | grep -oE "$pattern" | head -n 1 || true)"
        if [[ -n "$value" ]]; then
            printf '%s\n' "$value"
            return 0
        fi
        sleep 1
    done

    echo "Timed out while waiting for $label from the $service service." >&2
    sanitize_logs "$service" >&2
    return 1
}

echo "Starting Stripe CLI and Cloudflare Tunnel as detached Compose services..."
docker compose --profile integrations up -d --force-recreate stripe-cli tunnel

stripe_webhook_secret="$(wait_for_value \
    stripe-cli \
    'whsec_[A-Za-z0-9_-]+' \
    'the Stripe webhook signing secret')"

public_webhook_base_url="$(wait_for_value \
    tunnel \
    'https://[a-z0-9-]+\.trycloudflare\.com' \
    'the Cloudflare Tunnel URL')"
public_webhook_host="${public_webhook_base_url#https://}"

ensure_local_env
update_local_env .env STRIPE_WEBHOOK_SECRET "$stripe_webhook_secret"
update_local_env .env PUBLIC_WEBHOOK_BASE_URL "$public_webhook_base_url"
update_local_env .env PUBLIC_BASE_URL "$public_webhook_base_url"

allowed_hosts="${ALLOWED_HOSTS:-localhost}"
if [[ "$allowed_hosts" != "*" && ";$allowed_hosts;" != *";$public_webhook_host;"* ]]; then
    allowed_hosts="${allowed_hosts%;};$public_webhook_host"
    update_local_env .env ALLOWED_HOSTS "$allowed_hosts"
fi

restart_local_app

echo "Stripe CLI and Cloudflare Tunnel are running in the background."
echo "STRIPE_WEBHOOK_SECRET was updated in the local .env."
echo "PUBLIC_WEBHOOK_BASE_URL was updated in the local .env."
echo "PUBLIC_BASE_URL uses the current webhook tunnel host."
echo "ALLOWED_HOSTS includes the current webhook tunnel host."
echo "Vipps webhook URL: ${public_webhook_base_url%/}/api/payments/vipps/webhook"
echo "Use 'docker compose --profile integrations ps' to check them."
