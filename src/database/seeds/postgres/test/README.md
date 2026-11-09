# PostgreSQL Finance test seed

This directory contains deterministic local test data for the Finance
PostgreSQL database. It does not contain production data or production
secrets.

The main entry point is `00_gamma_finance_seed.sql`. It includes the metadata
seed and the materialized Finance test data. The metadata-only entry point is
`00_gamma_finance_metadata_seed.sql`.

The seed is intended for local development and testing only. The local Compose
seed service checks that the target tables are empty and records successful
execution before it finishes.
