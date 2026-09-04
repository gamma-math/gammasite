# SQL

This folder contains database schema scripts, views, and test seed data for GamMaSite.

## Structure

Only tracked project folders/files are listed here:

```text
SQL/
|-- test-seed/                     Ordered seed scripts for Identity, content, registrations, and templates.
|-- views/                         SQL view definitions.
|-- AspNetMysql.sql                Main database schema script.
`-- AspNetMysql.test.sql           Test/staging database schema script.
```

## Seed Data

The test seed scripts are ordered by numeric prefix and can be run individually or through:

```text
SQL/test-seed/00_seed_all.sql
```

Current content-related seed scripts include:

* `13_ContentItems.sql`
* `14_ContentLinks.sql`
* `15_EventRegistrations.sql`
* `16_EmailTemplates.sql`

Run schema/table scripts before seed scripts. Insert parent data before dependent data, for example `ContentItems` before `ContentLinks` and `EventRegistrations`.

## Notes

* The seed data is intended for local/test environments, not production user data.
* Keep scripts idempotent where practical so they can be re-run safely during test setup.
* Avoid documenting or relying on local-only database exports that are not tracked in Git.
