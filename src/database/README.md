# Database schema management

The SQL files in `src/database/mysql/` and `src/database/postgres/` are the
schema Source of Truth. The Python CLI reads those definitions, compares them
with live metadata and creates an additive deployment plan. It never creates
`DROP TABLE`, `DROP COLUMN` or data migration statements automatically.

Install the database service dependencies from the repository root:

```powershell
py -m venv src/services/database/.venv
& .\src\services\database\.venv\Scripts\python.exe -m pip install -r src/services/database/requirements.txt
```

Examples:

```powershell
& .\src\services\database\.venv\Scripts\python.exe -m src.services.database diff --db mysql --env local
& .\src\services\database\.venv\Scripts\python.exe -m src.services.database validate --db postgres --env prod
& .\src\services\database\.venv\Scripts\python.exe -m src.services.database plan --db mysql --env test
& .\src\services\database\.venv\Scripts\python.exe -m src.services.database deploy --db mysql --env local --confirm
```

Connection resolution uses `DBMANAGER_<DB>_<ENV>_CONNECTION_STRING` first. The
website workflow maps MySQL's existing `CONNECTION_STRING` and PostgreSQL's
dedicated `FINANCE_CONNECTION_STRING` into the CLI. Local MySQL uses
`127.0.0.1:3306`; the Finance PostgreSQL host port is read from
`FINANCE_POSTGRES_PORT` in the local `.env` file (5433 by default).

`deploy` requires `--confirm`; production additionally requires
`--confirm-production`. Unique indexes, constraints, views, PostgreSQL
functions and triggers require `--allow-risk`. CHECK constraints are included
in schema validation and additive deployment plans. Extra objects,
incompatible column changes and changed constraints, functions or triggers are
reported and blocked for manual handling.

PostgreSQL deployments execute in one transaction. MySQL DDL is not atomic:
successful statements are committed individually, and a later failure reports
how many statements were already applied. Run `validate` before retrying a
failed MySQL deployment.
