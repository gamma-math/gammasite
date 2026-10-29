#!/usr/bin/env bash

load_local_env() {
    local env_file=".env"
    if [[ ! -f "$env_file" ]]; then
        env_file=".env.example"
    fi
    if [[ ! -f "$env_file" ]]; then
        echo "Missing .env.example in $PWD." >&2
        return 1
    fi

    # Read both Unix and Windows line endings without executing values as shell
    # code. Compose values such as ALLOWED_HOSTS may contain semicolons.
    while IFS= read -r line || [[ -n "$line" ]]; do
        line="${line%$'\r'}"
        [[ -z "${line//[[:space:]]/}" || "$line" =~ ^[[:space:]]*# ]] && continue

        if [[ "$line" =~ ^[[:space:]]*(export[[:space:]]+)?([A-Za-z_][A-Za-z0-9_]*)=(.*)$ ]]; then
            local key="${BASH_REMATCH[2]}"
            local value="${BASH_REMATCH[3]}"

            if [[ "$value" == \"*\" && "$value" == *\" ]]; then
                value="${value:1:${#value}-2}"
            elif [[ "$value" == \'*\' && "$value" == *\' ]]; then
                value="${value:1:${#value}-2}"
            fi

            export "$key=$value"
        fi
    done < "$env_file"
}

ensure_local_env() {
    if [[ ! -f .env ]]; then
        cp .env.example .env
    fi
}

update_local_env() {
    local file="$1"
    local key="$2"
    local value="$3"
    local temporary_file

    temporary_file="$(mktemp)"
    if ! LOCAL_ENV_UPDATE_VALUE="$value" awk -v key="$key" '
        BEGIN {
            found = 0
            pattern = "^[[:space:]]*(export[[:space:]]+)?" key "="
        }
        $0 ~ pattern {
            print key "=" ENVIRON["LOCAL_ENV_UPDATE_VALUE"]
            found = 1
            next
        }
        { print }
        END {
            if (!found) {
                print key "=" ENVIRON["LOCAL_ENV_UPDATE_VALUE"]
            }
        }
    ' "$file" > "$temporary_file"; then
        rm -f -- "$temporary_file"
        return 1
    fi

    chmod --reference="$file" "$temporary_file" 2>/dev/null || chmod 600 "$temporary_file"
    mv -- "$temporary_file" "$file"
}

restart_local_app() {
    docker compose up -d --no-deps --force-recreate app
}
