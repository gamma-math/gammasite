# Local Docker development

This setup runs GamMaSite with MySQL, Finance PostgreSQL and the existing
backend/frontend application.

Helper commands are collected in [`scripts/`](./scripts/), and PostgreSQL
initialization files are in [`postgres-init/`](./postgres-init/).

## Requirements

- Docker Engine or Docker Desktop with Compose v2.
- Vipps credentials only when Vipps webhooks must be registered or rotated.

All helper and integration scripts can run inside Docker. Windows users do not
need Git for Windows for the documented workflows.

## Quick start

Run the commands from this directory:

```bash
docker compose config --quiet
docker compose up -d --build
docker compose ps
docker compose --profile tools run --rm --build tools scripts/validate.sh
```

The Compose file provides local defaults, so copying `.env.example` to `.env`
is optional. Use `.env` for local overrides and test credentials only; never
put production secrets in it.

Local endpoints:

- GamMaSite: `http://localhost:5000`
- MySQL: `localhost:3306`
- Finance PostgreSQL: `localhost:5433`

Set `MYSQL_PORT` or `FINANCE_POSTGRES_PORT` in `.env` if either port is already
in use. Internal application connections continue to use Compose service names
and container ports.

## Test data

Test data is never loaded during normal startup. Run the seed service only when
the local databases are empty:

```bash
docker compose --profile tools run --rm --build seed
```

The seed service checks whether seeding has already been recorded and refuses
to seed relevant tables that already contain data. It also normalizes relevant
local MySQL tables to `utf8mb4_general_ci` before seeding.

Seed source files:

- [`src/database/seeds/mysql/test/`](../../database/seeds/mysql/test/)
- [`src/database/seeds/postgres/test/`](../../database/seeds/postgres/test/)

Schema source-of-truth files:

- [`src/database/mysql/gam_ma_dk_db.sql`](../../database/mysql/gam_ma_dk_db.sql)
- [`src/database/postgres/finance.sql`](../../database/postgres/finance.sql)

The seed service is a one-shot container and does not create a permanent
service.

## Database lifecycle and safety

Schema initialization scripts run automatically only when a database volume is
created for the first time. Rebuilding the application image does not recreate
database volumes. Do not use `docker compose down -v` unless all local database
and media data may be deleted.

Stop the stack without deleting volumes:

```bash
docker compose down
```

Use the shared schema-management service for local validation and deployment:

```bash
python -m src.services.database validate --db mysql --env local
python -m src.services.database validate --db postgres --env local
python -m src.services.database plan --db mysql --env local
python -m src.services.database deploy --db mysql --env local --confirm
```

The same implementation is used by GitHub Actions. Production deployment
requires an additional explicit production confirmation.

## Rebuild and test the application

Rebuild only the application when database containers are already running:

```bash
docker compose up -d --build --no-deps app
```

For a clean application image:

```bash
docker compose build --no-cache app
docker compose up -d --no-deps app
```

Run frontend and backend tests as one-shot services:

```powershell
docker compose --profile tests run --rm --build frontend-tests
docker compose --profile tests run --rm --build backend-tests
```

## Stripe and Vipps integrations

The standard startup does not require external integrations. Start Stripe CLI
and the Cloudflare Quick Tunnel only when needed:

```bash
docker compose --profile tools run --rm --build tools scripts/integrations-up.sh
```

This updates the local `.env` with the active Stripe webhook secret, tunnel URL
and allowed host, then recreates only the application container. The tunnel URL
is temporary and must not be used as a permanent Test or Production webhook.

Register or rotate a Vipps webhook with:

```bash
docker compose --profile tools run --rm --build tools scripts/vipps-webhook.sh register
docker compose --profile tools run --rm --build tools scripts/vipps-webhook.sh rotate
```

Inspect or stop integration services with:

```bash
docker compose --profile integrations ps
docker compose --profile integrations logs -f stripe-cli tunnel
docker compose --profile integrations stop stripe-cli tunnel
```

Use Stripe test keys and Vipps test credentials only. The server-side Stripe
key needs the permissions required by the application and Stripe CLI; the
publishable key does not need server permissions.

## Validation and troubleshooting

Run the local validation script whenever the stack is running:

```bash
docker compose --profile tools run --rm --build tools scripts/validate.sh
```

It checks Compose configuration, container health, database connectivity and
the local HTTP endpoint without printing secrets or connection strings.

Common issues:

- If Docker reports that `dockerDesktopLinuxEngine` cannot be found, start
  Docker Desktop and verify that the Linux engine is running.
- If port `3306` or `5433` is unavailable, change `MYSQL_PORT` or
  `FINANCE_POSTGRES_PORT` in `.env`.
- If a database is unhealthy, inspect `docker compose logs mysql` or
  `docker compose logs finance-postgres`.
- If the application does not start, inspect `docker compose logs app`; it
  waits for both database health checks.
- If MySQL reports a collation conflict during seeding, verify that the seed
  tables are empty. The seed service performs the required local normalization;
  database volumes do not need to be deleted.
- If a webhook is unauthorized, verify the local secret, registered URL and
  the status of the integration containers.

Mailpit is not included because the current email service sends directly to
Mailgun's HTTP API. Supporting Mailpit would require a configurable SMTP
provider and is outside this local runner.
