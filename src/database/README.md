# Database schema management

The SQL files in `src/database/mysql/` and `src/database/postgres/` are the
schema Source of Truth. The Python CLI reads those definitions, compares them
with live metadata and creates an additive deployment plan. It never creates
`DROP TABLE`, `DROP COLUMN` or data migration statements automatically.

Install the database drivers from the repository root:

```powershell
& C:/Users/Bruger/AppData/Local/Microsoft/WindowsApps/python3.13.exe -m pip install -r src/database/requirements.txt
```

Examples:

```powershell
python src/database/scripts/cli.py diff --db mysql --env local
python src/database/scripts/cli.py validate --db postgres --env prod
python src/database/scripts/cli.py plan --db mysql --env test
python src/database/scripts/cli.py deploy --db mysql --env local --confirm
```

Connection resolution uses `DBMANAGER_<DB>_<ENV>_CONNECTION_STRING` first. The
website workflow maps MySQL's existing `CONNECTION_STRING` and PostgreSQL's
dedicated `FINANCE_CONNECTION_STRING` into the CLI. Local defaults match the
existing Docker Compose service (`127.0.0.1:3306` and `127.0.0.1:5433`).

`deploy` requires `--confirm`; production additionally requires
`--confirm-production`. Unique indexes, constraints and views require
`--allow-risk`. Extra objects, incompatible column changes and changed
constraints are reported and blocked for manual handling.
