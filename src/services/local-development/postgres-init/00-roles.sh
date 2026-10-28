#!/usr/bin/env bash
set -euo pipefail

: "${FINANCE_READ_PASSWORD:?FINANCE_READ_PASSWORD is required}"
: "${FINANCE_WRITE_PASSWORD:?FINANCE_WRITE_PASSWORD is required}"

psql \
  --set=finance_read_password="$FINANCE_READ_PASSWORD" \
  --set=finance_write_password="$FINANCE_WRITE_PASSWORD" \
  --username "$POSTGRES_USER" \
  --dbname "$POSTGRES_DB" \
  --set ON_ERROR_STOP=1 <<'SQL'
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'gamma_finance_read') THEN
        CREATE ROLE gamma_finance_read
            LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;
    END IF;

    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'gamma_finance_write') THEN
        CREATE ROLE gamma_finance_write
            LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE INHERIT;
    END IF;
END
$$;

ALTER ROLE gamma_finance_read PASSWORD :'finance_read_password';
ALTER ROLE gamma_finance_write PASSWORD :'finance_write_password';
SQL
