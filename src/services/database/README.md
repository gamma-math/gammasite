# Local database management service

This lightweight service exposes the shared implementation in
`src/database/scripts/` without creating a second schema-management engine.
It is intended for local MySQL/PostgreSQL testing and uses the same CLI as
GitHub Actions.

From the repository root:

```powershell
docker compose -f src/services/local-development/compose.yml up -d mysql finance-postgres
python -m src.services.database validate --db mysql --env local
python -m src.services.database validate --db postgres --env local
python -m src.services.database plan --db mysql --env local
python -m src.services.database deploy --db mysql --env local --confirm
```

The local connection defaults are `127.0.0.1:3306` for MySQL and
`127.0.0.1:5433` for Finance PostgreSQL. Remote Test/Production connections
must be supplied explicitly through the documented environment variables, and
Production deployment additionally requires `--confirm-production`.
