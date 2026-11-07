# Local database management service

This lightweight service exposes the shared implementation in
`src/database/scripts/` without creating a second schema-management engine.
It is intended for local MySQL/PostgreSQL testing and uses the same CLI as
GitHub Actions.

Set up the service-local virtual environment once from the repository root:

```powershell
py -m venv src/services/database/.venv
& .\src\services\database\.venv\Scripts\python.exe -m pip install -r src/services/database/requirements.txt
```

Run the service from the repository root:

```powershell
docker compose -f src/services/local-development/compose.yml up -d mysql finance-postgres
& .\src\services\database\.venv\Scripts\python.exe -m src.services.database validate --db mysql --env local
& .\src\services\database\.venv\Scripts\python.exe -m src.services.database validate --db postgres --env local
& .\src\services\database\.venv\Scripts\python.exe -m src.services.database plan --db mysql --env local
& .\src\services\database\.venv\Scripts\python.exe -m src.services.database deploy --db mysql --env local --confirm
```

The local connection defaults are `127.0.0.1:3306` for MySQL and the
`FINANCE_POSTGRES_PORT` configured in the local `.env` file for Finance
PostgreSQL (5433 by default). Remote Test/Production connections must be
supplied explicitly through the documented environment variables, and
Production deployment additionally requires `--confirm-production`.
