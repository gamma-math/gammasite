-- Creates the two login roles used by the GamMaSite finance backend.
-- Run this as the database owner after PostgreSQLTest.sql:
--
--   psql -v finance_read_password='...' -v finance_write_password='...' \
--     -f SQL/finance/CreateFinanceLoginRoles.sql <neon connection options>
--
-- Do not commit production passwords. The roles themselves and their table
-- privileges are defined in PostgreSQLTest.sql.

\if :{?finance_read_password}
\else
\prompt 'Password for gamma_finance_read: ' finance_read_password
\endif

\if :{?finance_write_password}
\else
\prompt 'Password for gamma_finance_write: ' finance_write_password
\endif

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'gamma_finance_read') THEN
        CREATE ROLE gamma_finance_read NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'gamma_finance_write') THEN
        CREATE ROLE gamma_finance_write NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE INHERIT;
    END IF;
END
$$;

ALTER ROLE gamma_finance_read LOGIN PASSWORD :'finance_read_password';
ALTER ROLE gamma_finance_write LOGIN PASSWORD :'finance_write_password';

-- These are Supabase compatibility roles from the former setup. They are not
-- GamMaSite users and are not required by Neon or the backend.
DROP ROLE IF EXISTS anon;
DROP ROLE IF EXISTS authenticated;

-- This application has no direct browser-to-database access. It therefore uses
-- ordinary PostgreSQL privileges instead of Supabase roles or RLS policies.
-- DISABLE is harmless for a new schema and migrates existing local databases
-- that previously enabled RLS without policies.
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
