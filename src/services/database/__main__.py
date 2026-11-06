"""Run the shared schema-management CLI as a local service."""

from src.database.scripts.cli import main


if __name__ == "__main__":
    raise SystemExit(main())
