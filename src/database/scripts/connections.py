"""Database connection resolution for the schema management CLI.

The module deliberately keeps credentials out of logs and supports both the
semicolon-separated .NET connection strings already used by GamMaSite and
normal database URLs. Read-only commands use a read-only session so a schema
validation cannot accidentally mutate a remote database.
"""

from __future__ import annotations

from dataclasses import dataclass, field
import os
from pathlib import Path
import re
from typing import Any
from urllib.parse import parse_qsl, unquote, urlsplit


class ConnectionConfigurationError(RuntimeError):
    """Raised when a database connection cannot be resolved safely."""


@dataclass(frozen=True)
class ConnectionSettings:
    """Normalized connection settings without exposing the password."""

    database: str
    environment: str
    scheme: str
    host: str
    port: int
    database_name: str
    user: str
    password: str
    options: dict[str, str] = field(default_factory=dict)
    source_name: str = ""

    @property
    def is_postgres(self) -> bool:
        return self.database == "postgres"

    @property
    def is_mysql(self) -> bool:
        return self.database == "mysql"

    @property
    def redacted(self) -> str:
        """Return safe diagnostic text with no password."""

        return (
            f"{self.database}://{self.user}:***@{self.host}:{self.port}/"
            f"{self.database_name} ({self.environment}; source={self.source_name})"
        )

    @property
    def conninfo(self) -> str:
        """Build psycopg conninfo for semicolon-style PostgreSQL settings."""

        if self.scheme.startswith("postgres") and self.options.get("raw_url"):
            return self.options["raw_url"]

        values = {
            "host": self.host,
            "port": str(self.port),
            "dbname": self.database_name,
            "user": self.user,
            "password": self.password,
        }
        for key in ("sslmode", "channel_binding", "connect_timeout"):
            if key in self.options:
                values[key] = self.options[key]

        return " ".join(f"{key}={_quote_conninfo(value)}" for key, value in values.items())


def parse_connection_string(
    value: str,
    database: str,
    environment: str,
    source_name: str = "explicit",
) -> ConnectionSettings:
    """Parse a database URL or a .NET-style connection string."""

    if not value or not value.strip():
        raise ConnectionConfigurationError("The database connection string is empty.")

    database = database.lower()
    environment = environment.lower()
    raw = value.strip()

    if "://" in raw:
        return _parse_url(raw, database, environment, source_name)

    pairs = _parse_semicolon_pairs(raw)
    normalized = {_normalize_key(key): _strip_quotes(val) for key, val in pairs.items()}
    host = normalized.get("host") or normalized.get("server") or normalized.get("data source")
    database_name = normalized.get("database") or normalized.get("initial catalog") or normalized.get("dbname")
    user = normalized.get("username") or normalized.get("user") or normalized.get("uid") or normalized.get("user id")
    password = normalized.get("password") or normalized.get("pwd") or ""

    if not host or not database_name or not user:
        raise ConnectionConfigurationError(
            "Connection string must contain host/server, database and username/user."
        )

    default_port = 5432 if database == "postgres" else 3306
    try:
        port = int(normalized.get("port", default_port))
    except ValueError as exc:
        raise ConnectionConfigurationError("Database port must be an integer.") from exc

    options: dict[str, str] = {}
    option_aliases = {
        "ssl mode": "sslmode",
        "sslmode": "sslmode",
        "channel binding": "channel_binding",
        "channelbinding": "channel_binding",
        "connect timeout": "connect_timeout",
        "connect_timeout": "connect_timeout",
    }
    for key, val in normalized.items():
        if key in option_aliases:
            options[option_aliases[key]] = val

    return ConnectionSettings(
        database=database,
        environment=environment,
        scheme="postgresql" if database == "postgres" else "mysql",
        host=host,
        port=port,
        database_name=database_name,
        user=user,
        password=password,
        options=options,
        source_name=source_name,
    )


def resolve_connection(
    database: str,
    environment: str,
    explicit: str | None = None,
    connection_env: str | None = None,
) -> ConnectionSettings:
    """Resolve a safe connection without ever guessing a remote credential.

    PostgreSQL explicitly supports ``FINANCE_CONNECTION_STRING``. MySQL
    supports the existing ``CONNECTION_STRING`` used by the website pipeline.
    More specific ``DBMANAGER_*`` names win when supplied.
    """

    database = database.lower()
    environment = environment.lower()
    if database not in {"mysql", "postgres"}:
        raise ConnectionConfigurationError(f"Unsupported database: {database}")
    if environment not in {"local", "test", "prod"}:
        raise ConnectionConfigurationError(f"Unsupported environment: {environment}")

    if explicit:
        return parse_connection_string(explicit, database, environment, "--connection")

    candidate_names = []
    if connection_env:
        candidate_names.append(connection_env)
    candidate_names.extend(
        [
            f"DBMANAGER_{database.upper()}_{environment.upper()}_CONNECTION_STRING",
            f"DBMANAGER_{database.upper()}_CONNECTION_STRING",
        ]
    )
    if database == "postgres":
        # Schema validation must use the dedicated database account, not the
        # application's read/write connection strings.
        candidate_names.append("FINANCE_CONNECTION_STRING")
    else:
        candidate_names.extend(
            [
                "CONNECTION_STRING",
                "MYSQL_CONNECTION_STRING",
                f"MYSQL_{environment.upper()}_CONNECTION_STRING",
                "SIMPLY_TEST_MYSQL_CONNECTION_STRING",
            ]
        )

    seen: set[str] = set()
    for name in candidate_names:
        if name in seen:
            continue
        seen.add(name)
        value = os.environ.get(name)
        if value and value.strip():
            return parse_connection_string(value, database, environment, name)

    local_settings = _resolve_local_components(database, environment)
    if local_settings is not None:
        return local_settings

    raise ConnectionConfigurationError(
        f"No connection configured for {database}/{environment}. "
        f"Set {candidate_names[0]} or pass --connection-env."
    )


def connect(settings: ConnectionSettings, read_only: bool = False) -> Any:
    """Open a DB-API connection, importing only the selected driver."""

    if settings.is_mysql:
        try:
            import pymysql
        except ImportError as exc:  # pragma: no cover - depends on installation
            raise ConnectionConfigurationError(
                "PyMySQL is required for MySQL. Install src/database/requirements.txt."
            ) from exc

        kwargs: dict[str, Any] = {
            "host": settings.host,
            "port": settings.port,
            "user": settings.user,
            "password": settings.password,
            "database": settings.database_name,
            "autocommit": False,
        }
        if settings.options.get("sslmode", "").lower() in {"disable", "none"}:
            kwargs["ssl_disabled"] = True
        connection = pymysql.connect(**kwargs)
    else:
        try:
            import psycopg
        except ImportError as exc:  # pragma: no cover - depends on installation
            raise ConnectionConfigurationError(
                "psycopg is required for PostgreSQL. Install src/database/requirements.txt."
            ) from exc
        connection = psycopg.connect(settings.conninfo)

    if read_only:
        cursor = connection.cursor()
        try:
            if settings.is_postgres:
                cursor.execute("SET SESSION CHARACTERISTICS AS TRANSACTION READ ONLY")
            else:
                cursor.execute("SET SESSION TRANSACTION READ ONLY")
            connection.commit()
        finally:
            cursor.close()
    return connection


def _parse_url(raw: str, database: str, environment: str, source_name: str) -> ConnectionSettings:
    parsed = urlsplit(raw)
    scheme = parsed.scheme.lower()
    if database == "mysql" and not scheme.startswith("mysql"):
        raise ConnectionConfigurationError("MySQL connections must use a mysql:// URL.")
    if database == "postgres" and scheme not in {"postgres", "postgresql"} and not scheme.startswith("postgres"):
        raise ConnectionConfigurationError("PostgreSQL connections must use a postgres:// URL.")
    if not parsed.hostname or not parsed.path.strip("/") or not parsed.username:
        raise ConnectionConfigurationError("Database URL must contain host, database and username.")

    options = {key.lower().replace("-", "_"): val for key, val in parse_qsl(parsed.query)}
    options["raw_url"] = raw
    return ConnectionSettings(
        database=database,
        environment=environment,
        scheme=scheme,
        host=parsed.hostname,
        port=parsed.port or (5432 if database == "postgres" else 3306),
        database_name=unquote(parsed.path.lstrip("/")),
        user=unquote(parsed.username),
        password=unquote(parsed.password or ""),
        options=options,
        source_name=source_name,
    )


def _parse_semicolon_pairs(raw: str) -> dict[str, str]:
    pairs: dict[str, str] = {}
    for part in raw.split(";"):
        if "=" not in part:
            continue
        key, value = part.split("=", 1)
        if key.strip():
            pairs[key.strip()] = value.strip()
    return pairs


def _normalize_key(key: str) -> str:
    return re.sub(r"\s+", " ", key.strip().lower().replace("_", " "))


def _strip_quotes(value: str) -> str:
    if len(value) >= 2 and value[0] == value[-1] and value[0] in {"'", '"'}:
        return value[1:-1]
    return value


def _quote_conninfo(value: str) -> str:
    if not value or re.search(r"\s|'", value):
        return "'" + value.replace("'", "\\'") + "'"
    return value


def _resolve_local_components(database: str, environment: str) -> ConnectionSettings | None:
    if environment != "local":
        return None

    if database == "mysql":
        host = os.environ.get("MYSQL_HOST", "127.0.0.1")
        port = os.environ.get("MYSQL_PORT", "3306")
        name = os.environ.get("MYSQL_DATABASE", "gammasite_local")
        user = os.environ.get("MYSQL_APP_RW_USER", os.environ.get("MYSQL_USER", "gammasite_app"))
        password = os.environ.get("MYSQL_APP_RW_PASSWORD", os.environ.get("MYSQL_PASSWORD", "change-me-local"))
    else:
        host = os.environ.get("FINANCE_POSTGRES_HOST", "127.0.0.1")
        port = os.environ.get("FINANCE_POSTGRES_PORT", "5433")
        name = os.environ.get("FINANCE_POSTGRES_DB", "gamma_finance_local")
        user = os.environ.get("FINANCE_POSTGRES_USER", "gamma_finance_write")
        password = os.environ.get("FINANCE_POSTGRES_PASSWORD", "change-me-local-finance-write")

    return parse_connection_string(
        f"Host={host};Port={port};Database={name};Username={user};Password={password};",
        database,
        environment,
        "local defaults",
    )

