-- Gamma Finance metadata/reference seed.
--
-- This runner intentionally seeds only non-transactional data:
-- account, postering_group, forecast and import_history.
--
-- It does not include bank_account, mobilepay or posteringer. Use
-- 00_gamma_finance_seed.sql when the transaction/source data is wanted too.

\set ON_ERROR_STOP on

\ir 01_account.sql
\ir 02_postering_group.sql
\ir 03_forecast.sql
\ir 07_import_history.sql
