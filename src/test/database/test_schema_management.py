"""Unit tests for schema parsing, comparison and deployment safety."""

from __future__ import annotations

from pathlib import Path
import unittest

from src.database.scripts.connections import parse_connection_string
from src.database.scripts.schema_compare import compare_schemas
from src.database.scripts.schema_deploy import build_plan, execute_plan
from src.database.scripts.schema_reader import (
    ColumnDefinition,
    SchemaDefinition,
    TableDefinition,
    parse_schema,
    read_schema_file,
)


ROOT = Path(__file__).resolve().parents[3]


class SchemaReaderTests(unittest.TestCase):
    def test_repository_mysql_schema_is_readable(self) -> None:
        schema = read_schema_file(ROOT / "src/database/mysql/gam_ma_dk_db.sql", "mysql")
        self.assertEqual(19, len(schema.tables))
        self.assertEqual(1, len(schema.views))
        self.assertIn("AspNetUsers", schema.tables)
        self.assertTrue(schema.tables["AspNetUsers"].columns["Id"].auto_increment is False)
        self.assertEqual(("MigrationId",), schema.indexes["__EFMigrationsHistory.PRIMARY"].columns)

    def test_repository_postgres_schema_is_readable(self) -> None:
        schema = read_schema_file(ROOT / "src/database/postgres/finance.sql", "postgres")
        self.assertEqual(9, len(schema.tables))
        self.assertEqual(25, len(schema.indexes))
        self.assertIn("public.posteringer.ix_posteringer_stripe_key", schema.indexes)
        self.assertTrue(schema.tables["public.bank_account"].columns["id"].auto_increment)

    def test_function_body_semicolons_do_not_split_schema(self) -> None:
        schema = parse_schema(
            """
            CREATE OR REPLACE FUNCTION public.touch() RETURNS trigger
            LANGUAGE plpgsql AS $$ BEGIN NEW.value = 1; RETURN NEW; END; $$;
            CREATE TABLE public.example (id bigint NOT NULL);
            """,
            "postgres",
        )
        self.assertEqual(["public.example"], sorted(schema.tables))


class SchemaComparisonTests(unittest.TestCase):
    def test_postgres_string_default_cast_matches_source_default(self) -> None:
        desired = parse_schema(
            "CREATE TABLE public.Example (value text DEFAULT 'safe');",
            "postgres",
        )
        actual = parse_schema(
            "CREATE TABLE public.Example (value text DEFAULT 'safe'::text);",
            "postgres",
        )

        self.assertTrue(compare_schemas(desired, actual).is_match)

    def test_missing_column_is_safe_additive_change(self) -> None:
        desired = _schema_with_column("new_column")
        actual = _schema_with_column()
        diff = compare_schemas(desired, actual)
        self.assertFalse(diff.is_match)
        self.assertFalse(diff.has_blocking_changes)
        plan = build_plan(desired, diff)
        self.assertIn("ADD COLUMN", plan.statements[0].sql)

    def test_matching_schemas_have_empty_idempotent_plan(self) -> None:
        schema = _schema_with_column("new_column")
        diff = compare_schemas(schema, schema)
        plan = build_plan(schema, diff)
        self.assertTrue(diff.is_match)
        self.assertTrue(plan.is_empty)

    def test_mysql_view_database_qualification_is_format_only(self) -> None:
        desired = _schema_with_view("SELECT `Example`.`id` FROM `Example`")
        actual = _schema_with_view("select `gammasite_local`.`Example`.`id` from `gammasite_local`.`Example`")
        diff = compare_schemas(desired, actual)
        self.assertTrue(diff.is_match)

    def test_incompatible_column_change_is_blocked(self) -> None:
        desired = _schema_with_column("id", data_type="bigint")
        actual = _schema_with_column("id", data_type="text")
        diff = compare_schemas(desired, actual)
        self.assertTrue(diff.has_blocking_changes)
        self.assertTrue(build_plan(desired, diff).blocked_reasons)

    def test_extra_table_is_never_deleted(self) -> None:
        desired = SchemaDefinition("mysql")
        actual = SchemaDefinition("mysql")
        actual.tables["Legacy"] = TableDefinition("Legacy", None, "")
        diff = compare_schemas(desired, actual)
        plan = build_plan(desired, diff)
        self.assertTrue(plan.blocked_reasons)
        self.assertNotIn("DROP", " ".join(statement.sql for statement in plan.statements).upper())


class ConnectionTests(unittest.TestCase):
    def test_postgres_dotnet_connection_string(self) -> None:
        settings = parse_connection_string(
            "Host=finance.example;Database=neondb;Username=finance;Password=secret;SSL Mode=VerifyFull;Channel Binding=Require;",
            "postgres",
            "prod",
            "FINANCE_CONNECTION_STRING",
        )
        self.assertEqual("finance.example", settings.host)
        self.assertEqual("verify-full", settings.options["sslmode"])
        self.assertEqual("require", settings.options["channel_binding"])
        self.assertIn("sslmode=verify-full", settings.conninfo)
        self.assertIn("channel_binding=require", settings.conninfo)
        self.assertNotIn("secret", settings.redacted)

    def test_mysql_dotnet_connection_string(self) -> None:
        settings = parse_connection_string(
            "Server=mysql.example;Database=gam_ma_dk_db;User=master;Password=secret;",
            "mysql",
            "test",
        )
        self.assertEqual(3306, settings.port)
        self.assertEqual("gam_ma_dk_db", settings.database_name)
        self.assertNotIn("secret", settings.redacted)


class DeploymentSafetyTests(unittest.TestCase):
    def test_mysql_auto_increment_primary_key_is_created_inline(self) -> None:
        desired = parse_schema(
            """
            CREATE TABLE `Example` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `Name` varchar(64) NOT NULL
            );
            ALTER TABLE `Example` ADD PRIMARY KEY (`Id`);
            """,
            "mysql",
        )
        actual = SchemaDefinition("mysql")

        plan = build_plan(desired, compare_schemas(desired, actual))

        create_table = next(
            statement
            for statement in plan.statements
            if statement.reason == "create missing table Example"
        )
        self.assertIn("PRIMARY KEY (`Id`)", create_table.sql)
        self.assertFalse(
            any("ADD PRIMARY KEY" in statement.sql for statement in plan.statements)
        )

    def test_primary_constraints_are_added_before_foreign_keys(self) -> None:
        desired = parse_schema(
            """
            CREATE TABLE `Parent` (`Id` varchar(32) NOT NULL);
            CREATE TABLE `Child` (`Id` int NOT NULL, `ParentId` varchar(32) NOT NULL);
            ALTER TABLE `Parent` ADD PRIMARY KEY (`Id`);
            ALTER TABLE `Child`
                ADD CONSTRAINT `FK_Child_Parent`
                FOREIGN KEY (`ParentId`) REFERENCES `Parent` (`Id`);
            """,
            "mysql",
        )
        actual = SchemaDefinition("mysql")

        plan = build_plan(desired, compare_schemas(desired, actual))
        primary_position = next(
            index
            for index, statement in enumerate(plan.statements)
            if "add missing primary constraint Parent.PRIMARY" in statement.reason
        )
        foreign_position = next(
            index
            for index, statement in enumerate(plan.statements)
            if "add missing foreign constraint Child.FK_Child_Parent" in statement.reason
        )

        self.assertLess(primary_position, foreign_position)

    def test_postgres_identity_columns_are_created_inline(self) -> None:
        desired = parse_schema(
            """
            CREATE TABLE public.Example (id bigint NOT NULL);
            ALTER TABLE public.Example
                ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY;
            ALTER TABLE public.Example ADD CONSTRAINT example_pkey PRIMARY KEY (id);
            """,
            "postgres",
        )
        actual = SchemaDefinition("postgres")

        plan = build_plan(desired, compare_schemas(desired, actual))

        create_table = next(
            statement
            for statement in plan.statements
            if statement.reason == "create missing table public.Example"
        )
        self.assertIn("GENERATED BY DEFAULT AS IDENTITY NOT NULL", create_table.sql)

    def test_postgres_inline_primary_key_is_not_emitted_twice(self) -> None:
        desired = parse_schema(
            """
            CREATE TABLE public.Example (
                id bigint GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                Name text NOT NULL,
                PRIMARY KEY (id)
            );
            """,
            "postgres",
        )
        actual = SchemaDefinition("postgres")

        plan = build_plan(desired, compare_schemas(desired, actual))

        self.assertEqual(
            ["create missing table public.Example"],
            [statement.reason for statement in plan.statements],
        )
        self.assertIn(
            "public.Example.Example_pkey",
            desired.constraints,
        )

    def test_deploy_requires_explicit_confirmation(self) -> None:
        desired = _schema_with_column("new_column")
        actual = _schema_with_column()
        plan = build_plan(desired, compare_schemas(desired, actual))
        with self.assertRaises(Exception):
            execute_plan(_FakeConnection(), plan)
        self.assertEqual([], _FakeConnection().executed)


def _schema_with_column(name: str | None = None, data_type: str = "text") -> SchemaDefinition:
    schema = SchemaDefinition("mysql")
    table = TableDefinition("Example", None, "CREATE TABLE `Example` (`id` text NOT NULL);")
    table.columns["id"] = ColumnDefinition("id", data_type, False, None, False, f"`id` {data_type} NOT NULL")
    if name and name != "id":
        table.columns[name] = ColumnDefinition(name, "text", True, None, False, f"`{name}` text")
    schema.tables["Example"] = table
    return schema


def _schema_with_view(definition: str) -> SchemaDefinition:
    schema = SchemaDefinition("mysql")
    from src.database.scripts.schema_reader import ViewDefinition

    schema.views["ExampleView"] = ViewDefinition("ExampleView", None, definition, "")
    return schema


class _FakeConnection:
    def __init__(self) -> None:
        self.executed: list[str] = []

    def cursor(self) -> "_FakeCursor":
        return _FakeCursor(self)

    def commit(self) -> None:
        raise AssertionError("commit must not be called without confirmation")

    def rollback(self) -> None:
        pass


class _FakeCursor:
    def __init__(self, connection: _FakeConnection) -> None:
        self.connection = connection

    def execute(self, sql: str) -> None:
        self.connection.executed.append(sql)

    def close(self) -> None:
        pass


if __name__ == "__main__":
    unittest.main()
