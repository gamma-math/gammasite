#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
RUNNER_DIR="$(cd -- "$SCRIPT_DIR/.." && pwd)"
REPOSITORY_ROOT="$(cd -- "$SCRIPT_DIR/../../../.." && pwd)"
VIPPS_COMPOSE_FILE="$REPOSITORY_ROOT/src/services/vipps/docker-compose.yml"
cd "$RUNNER_DIR"

if ! command -v docker >/dev/null 2>&1; then
    echo "Docker is required for the Vipps webhook helper." >&2
    exit 2
fi

# shellcheck disable=SC1091
source "$SCRIPT_DIR/_env.sh"
load_local_env

vipps_credentials_args=(
    -e "VIPPS_BASE_URL=${VIPPS_BASE_URL:-}"
    -e "VIPPS_MSN=${VIPPS_MSN:-}"
    -e "VIPPS_CLIENT_ID=${VIPPS_CLIENT_ID:-}"
    -e "VIPPS_CLIENT_SECRET=${VIPPS_CLIENT_SECRET:-}"
    -e "VIPPS_OCP_APIM_SUBSCRIPTION_KEY=${VIPPS_OCP_APIM_SUBSCRIPTION_KEY:-}"
)

usage() {
    echo "Usage:"
    echo "  ./scripts/vipps-webhook.sh list"
    echo "  ./scripts/vipps-webhook.sh register [--url PUBLIC_WEBHOOK_URL]"
    echo "  ./scripts/vipps-webhook.sh rotate OLD_WEBHOOK_ID [--url PUBLIC_WEBHOOK_URL]"
    echo "  ./scripts/vipps-webhook.sh delete OLD_WEBHOOK_ID"
}

operation="${1:-}"
shift || true

if [[ "$operation" == "list" ]]; then
    if [[ $# -gt 0 ]]; then
        usage >&2
        exit 2
    fi

    exec docker compose \
        -f "$VIPPS_COMPOSE_FILE" \
        --profile manual \
        run --rm --build "${vipps_credentials_args[@]}" webhooks
fi

old_webhook_id=""
webhook_url="${VIPPS_WEBHOOK_URL:-}"

if [[ "$operation" == "rotate" || "$operation" == "delete" ]]; then
    old_webhook_id="${1:-}"
    shift || true
    if [[ -z "$old_webhook_id" ]]; then
        usage >&2
        exit 2
    fi
elif [[ "$operation" != "register" ]]; then
    usage >&2
    exit 2
fi

while [[ $# -gt 0 ]]; do
    case "$1" in
        --url)
            if [[ $# -lt 2 ]]; then
                echo "--url requires a value." >&2
                exit 2
            fi
            webhook_url="$2"
            shift 2
            ;;
        *)
            echo "Unknown argument: $1" >&2
            usage >&2
            exit 2
            ;;
    esac
done

if [[ "$operation" != "delete" && -z "$webhook_url" && -n "${PUBLIC_WEBHOOK_BASE_URL:-}" ]]; then
    webhook_url="${PUBLIC_WEBHOOK_BASE_URL%/}/api/payments/vipps/webhook"
fi

if [[ "$operation" != "delete" && -z "$webhook_url" ]]; then
    echo "A public Vipps webhook URL is required. Start integrations-up.sh first or provide --url." >&2
    exit 2
fi

log_file="$(mktemp)"
cleanup() {
    rm -f -- "$log_file"
}
trap cleanup EXIT

compose_args=(
    -f "$VIPPS_COMPOSE_FILE"
    --profile manual
    run
    --rm
    --build
    "${vipps_credentials_args[@]}"
)

if [[ "$operation" == "register" ]]; then
    compose_args+=(
        -e "VIPPS_WEBHOOK_URL=$webhook_url"
        register-webhook
    )
elif [[ "$operation" == "rotate" ]]; then
    compose_args+=(
        -e "VIPPS_WEBHOOK_URL=$webhook_url"
        -e "VIPPS_OLD_WEBHOOK_ID=$old_webhook_id"
        rotate-webhook
    )
else
    compose_args+=(
        -e "VIPPS_OLD_WEBHOOK_ID=$old_webhook_id"
        delete-webhook
    )
fi

if ! docker compose "${compose_args[@]}" >"$log_file" 2>&1; then
    echo "Vipps webhook operation failed. Check Vipps credentials, permissions and the public URL." >&2
    echo "No local webhook secret was changed." >&2
    exit 1
fi

if [[ "$operation" == "delete" ]]; then
    echo "Vipps webhook was deleted: $old_webhook_id"
    exit 0
fi

webhook_secret="$(sed -nE 's/.*VIPPS_WEBHOOK_SECRET:[[:space:]]*([^[:space:]]+).*/\1/p' "$log_file" | tail -n 1 || true)"
if [[ -z "$webhook_secret" ]]; then
    echo "Vipps CLI completed without returning a webhook secret. No local webhook secret was changed." >&2
    exit 1
fi

ensure_local_env
update_local_env .env VIPPS_WEBHOOK_SECRET "$webhook_secret"
restart_local_app

echo "Vipps webhook was ${operation}ed for: $webhook_url"
echo "VIPPS_WEBHOOK_SECRET was updated in the local .env and the app container was restarted."
if [[ "$operation" == "rotate" ]]; then
    echo "Keep the old webhook until the replacement has been verified, then delete it through the existing Vipps CLI."
fi
