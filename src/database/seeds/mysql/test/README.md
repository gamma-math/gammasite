# MySQL test seed

This directory contains fake, deterministic test data for local GamMaSite
development and testing. It does not contain production data or production
secrets.

## Test accounts

- Admin: `admin.test@gamma.local` / `Admin123!Test`
- User: `bruger.test@gamma.local` / `Bruger123!Test`
- Other test users: `Bruger123!Test`

## Running the seed

The recommended entry point is `00_seed_all.sql`. The local Compose seed
service executes it after checking that the relevant tables are empty.

The numbered files can also be executed individually. Parent data must be
inserted before dependent data, for example `AspNetRoles` before users and
`ContentItems` before content links and event registrations.

Some scripts intentionally leave operational tables empty, including
`DeviceCodes`, `PersistedGrants`, signing keys and the user overview view.

These files are intended only for local development and test environments.
