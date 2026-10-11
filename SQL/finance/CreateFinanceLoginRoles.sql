-- Creates the two login roles used by the GamMaSite finance backend.
-- Run this as the database owner after PostgreSQLTest.sql:
--
--   psql -v finance_read_password='...' -v finance_write_password='...' \
--     -f SQL/finance/CreateFinanceLoginRoles.sql <neon connection options>
--
-- Do not commit production passwords. The roles themselves and their table
-- privileges are defined in this script; PostgreSQLTest.sql defines the
-- finance tables that are granted below.

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

-- This application has no direct browser-to-database access. It therefore uses
-- ordinary PostgreSQL privileges instead of Supabase roles or RLS policies.
-- The database is dedicated to finance, but grants are deliberately limited
-- to the tables used by the finance services.
ALTER TABLE public.account DISABLE ROW LEVEL SECURITY;
ALTER TABLE public.bank_account DISABLE ROW LEVEL SECURITY;
ALTER TABLE public.forecast DISABLE ROW LEVEL SECURITY;
ALTER TABLE public.mobilepay DISABLE ROW LEVEL SECURITY;
ALTER TABLE public.postering_group DISABLE ROW LEVEL SECURITY;
ALTER TABLE public.posteringer DISABLE ROW LEVEL SECURITY;
ALTER TABLE public.import_history DISABLE ROW LEVEL SECURITY;

REVOKE ALL ON TABLE
    public.account,
    public.bank_account,
    public.mobilepay,
    public.postering_group,
    public.forecast,
    public.posteringer,
    public.import_history
FROM PUBLIC;

REVOKE ALL ON SEQUENCE
    public.bank_account_id_seq,
    public.mobilepay_id_seq,
    public.import_history_id_seq
FROM PUBLIC;

DO $$
BEGIN
    EXECUTE format(
        'GRANT CONNECT ON DATABASE %I TO gamma_finance_read, gamma_finance_write',
        current_database()
    );
END
$$;

GRANT USAGE ON SCHEMA public TO gamma_finance_read, gamma_finance_write;
GRANT SELECT ON TABLE
    public.account,
    public.bank_account,
    public.mobilepay,
    public.postering_group,
    public.forecast,
    public.posteringer,
    public.import_history
TO gamma_finance_read;
GRANT USAGE, SELECT ON SEQUENCE
    public.bank_account_id_seq,
    public.mobilepay_id_seq,
    public.import_history_id_seq
TO gamma_finance_read;

GRANT gamma_finance_read TO gamma_finance_write;
GRANT INSERT, UPDATE, DELETE ON TABLE
    public.account,
    public.bank_account,
    public.mobilepay,
    public.postering_group,
    public.forecast,
    public.posteringer,
    public.import_history
TO gamma_finance_write;
GRANT USAGE, SELECT ON SEQUENCE
    public.bank_account_id_seq,
    public.mobilepay_id_seq,
    public.import_history_id_seq
TO gamma_finance_write;

-- If a new finance table or identity sequence is added, extend this explicit
-- grant list as part of the same schema change.
