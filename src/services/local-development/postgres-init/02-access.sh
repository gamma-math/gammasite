#!/usr/bin/env bash
set -euo pipefail

psql \
  --username "$POSTGRES_USER" \
  --dbname "$POSTGRES_DB" \
  --set ON_ERROR_STOP=1 <<'SQL'
ALTER TABLE public.account DISABLE ROW LEVEL SECURITY;
ALTER TABLE public.bank_account DISABLE ROW LEVEL SECURITY;
ALTER TABLE public.forecast DISABLE ROW LEVEL SECURITY;
ALTER TABLE public.mobilepay DISABLE ROW LEVEL SECURITY;
ALTER TABLE public.postering_group DISABLE ROW LEVEL SECURITY;
ALTER TABLE public.posteringer DISABLE ROW LEVEL SECURITY;
ALTER TABLE public.import_history DISABLE ROW LEVEL SECURITY;

REVOKE ALL ON SCHEMA public FROM PUBLIC;
REVOKE ALL ON ALL TABLES IN SCHEMA public FROM PUBLIC;
REVOKE ALL ON ALL SEQUENCES IN SCHEMA public FROM PUBLIC;

DO $$
BEGIN
    EXECUTE format(
        'GRANT CONNECT ON DATABASE %I TO gamma_finance_read, gamma_finance_write',
        current_database()
    );
END
$$;

GRANT USAGE ON SCHEMA public TO gamma_finance_read, gamma_finance_write;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO gamma_finance_read;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO gamma_finance_read;
GRANT gamma_finance_read TO gamma_finance_write;
GRANT INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO gamma_finance_write;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO gamma_finance_write;

ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT SELECT ON TABLES TO gamma_finance_read;
ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO gamma_finance_read;
ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT INSERT, UPDATE, DELETE ON TABLES TO gamma_finance_write;
ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO gamma_finance_write;
SQL
