"""Command-line entry point for database schema diff, validation and deploy."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
import sys
from typing import Sequence

if __package__ in {None, ""}:  # Support `python src/database/scripts/cli.py ...`.
    _repo_root = Path(__file__).resolve().parents[3]
    sys.path.insert(0, str(_repo_root))
    from src.database.scripts.connections import (  # type: ignore[import-not-found]
        ConnectionConfigurationError,
        connect,
        resolve_connection,
    )
    from src.database.scripts.schema_compare import compare_schemas, format_diff  # type: ignore[import-not-found]
    from src.database.scripts.schema_deploy import (  # type: ignore[import-not-found]
        DeploymentBlocked,
        build_plan,
        execute_plan,
        format_plan,
    )
    from src.database.scripts.schema_reader import read_database_schema, read_schema_file  # type: ignore[import-not-found]
else:
    from .connections import ConnectionConfigurationError, connect, resolve_connection
    from .schema_compare import compare_schemas, format_diff
    from .schema_deploy import DeploymentBlocked, build_plan, execute_plan, format_plan
    from .schema_reader import read_database_schema, read_schema_file


REPO_ROOT = Path(__file__).resolve().parents[3]
DEFAULT_SCHEMAS = {
    "mysql": REPO_ROOT / "src/database/mysql/gam_ma_dk_db.sql",
    "postgres": REPO_ROOT / "src/database/postgres/finance.sql",
}


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Compare and safely deploy GamMa MySQL/PostgreSQL schemas."
    )
    subparsers = parser.add_subparsers(dest="command", required=True)
    for command in ("diff", "validate", "plan", "deploy"):
        command_parser = subparsers.add_parser(command, help=_command_help(command))
        command_parser.add_argument("--db", choices=("mysql", "postgres"), required=True)
        command_parser.add_argument("--env", choices=("local", "test", "prod"), default="local")
        command_parser.add_argument("--schema", type=Path, help="Override the local SQL Source of Truth.")
        command_parser.add_argument("--connection", help="Connection URL/string; prefer environment variables.")
        command_parser.add_argument("--connection-env", help="Environment variable containing the connection string.")
        command_parser.add_argument("--format", choices=("text", "json"), default="text")
        if command == "deploy":
            command_parser.add_argument("--confirm", action="store_true", help="Confirm execution of the generated plan.")
            command_parser.add_argument("--allow-risk", action="store_true", help="Allow confirmed indexes, constraints and view changes.")
            command_parser.add_argument("--confirm-production", action="store_true", help="Required in addition to --confirm for prod.")
    return parser


def main(argv: Sequence[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    try:
        desired = read_schema_file(args.schema or DEFAULT_SCHEMAS[args.db], args.db)
        settings = resolve_connection(args.db, args.env, args.connection, args.connection_env)
        print(f"Connecting to {settings.redacted}", file=sys.stderr)
        connection = connect(settings, read_only=args.command != "deploy")
        try:
            actual = read_database_schema(connection, args.db)
            diff = compare_schemas(desired, actual)
            if args.command == "diff":
                _print_diff(diff, args.format)
                return 0 if diff.is_match else 1
            if args.command == "validate":
                _print_diff(diff, args.format)
                return 0 if diff.is_match else 1
            plan = build_plan(desired, diff)
            _print_plan(plan, args.format)
            if args.command == "plan":
                return 0
            if args.env == "prod" and not args.confirm_production:
                raise DeploymentBlocked(
                    "Production deploy requires --confirm-production in addition to --confirm."
                )
            execute_plan(
                connection,
                plan,
                confirm=args.confirm,
                allow_risky=args.allow_risk,
            )
            validated = compare_schemas(desired, read_database_schema(connection, args.db))
            if not validated.is_match:
                _print_diff(validated, args.format)
                raise DeploymentBlocked(
                    "Deployment completed, but post-deployment schema validation failed."
                )
            print("Deployment completed and post-deployment validation passed.")
        finally:
            connection.close()
        return 0
    except (ConnectionConfigurationError, DeploymentBlocked, OSError, ValueError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 2
    except Exception as exc:  # pragma: no cover - driver/network-specific safety net
        print(f"ERROR: database operation failed: {exc}", file=sys.stderr)
        return 2


def _print_diff(diff: object, output_format: str) -> None:
    if output_format == "text":
        print(format_diff(diff))  # type: ignore[arg-type]
        return
    differences = [difference.__dict__ for difference in diff.differences]  # type: ignore[attr-defined]
    print(json.dumps({"database": diff.database, "matches": diff.is_match, "differences": differences}, indent=2))  # type: ignore[attr-defined]


def _print_plan(plan: object, output_format: str) -> None:
    if output_format == "text":
        print(format_plan(plan))  # type: ignore[arg-type]
        return
    print(
        json.dumps(
            {
                "database": plan.database,  # type: ignore[attr-defined]
                "blocked": plan.blocked_reasons,  # type: ignore[attr-defined]
                "statements": [statement.__dict__ for statement in plan.statements],  # type: ignore[attr-defined]
            },
            indent=2,
        )
    )


def _command_help(command: str) -> str:
    return {
        "diff": "Show schema differences without modifying the database.",
        "validate": "Validate the database schema without modifying the database.",
        "plan": "Generate a deployment plan without executing SQL.",
        "deploy": "Execute an explicitly confirmed additive deployment plan.",
    }[command]


if __name__ == "__main__":
    raise SystemExit(main())

