#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPOSITORY_ROOT="$(cd -- "$SCRIPT_DIR/../../../.." && pwd)"
COMPOSE_FILE="$REPOSITORY_ROOT/src/services/vipps/docker-compose.yml"

if [[ $# -eq 0 ]]; then
    echo "Usage: ./scripts/mobilepay.sh --profile manual run --rm <service> [arguments...]" >&2
    exit 2
fi

exec docker compose -f "$COMPOSE_FILE" "$@"
