"""Unit tests for schema parsing, comparison and deployment safety."""

from __future__ import annotations

from pathlib import Path
import unittest

from src.database.scripts.connections import parse_connection_string
from src.database.scripts.schema_compare import compare_schemas
from src.database.scripts.schema_deploy import (
    DeploymentExecutionError,
    DeploymentPlan,
    PlannedStatement,
    build_plan,
    execute_plan,
)
from src.database.scripts.schema_reader import (
    ColumnDefinition,
    SchemaDefinition,
    TableDefinition,
    parse_schema,
    read_database_schema,
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
        generated = schema.tables["PaymentOffers"].columns["ContentItemScopeKey"]
        self.assertIsNotNone(generated.generation_expression)
        self.assertIn("case when", (generated.generation_expression or "").lower())

    def test_mysql_live_generated_column_uses_show_create_definition(self) -> None:
        schema = read_database_schema(_FakeMySqlMetadataConnection(), "mysql")
        generated = schema.tables["PaymentOffers"].columns["ContentItemScopeKey"]

        self.assertEqual(
            "((case when (`Scope` = _utf8mb4'MEMBERSHIP') then 0 else NULL end))",
            generated.generation_expression,
        )

    def test_repository_postgres_schema_is_readable(self) -> None:
        schema = read_schema_file(ROOT / "src/database/postgres/finance.sql", "postgres")
        self.assertEqual(9, len(schema.tables))
        self.assertEqual(25, len(schema.indexes))
        self.assertEqual(1, len(schema.functions))
        self.assertEqual(8, len(schema.triggers))
        self.assertIn("public.set_updated_at()", schema.functions)
        self.assertIn("public.posteringer.ix_posteringer_stripe_key", schema.indexes)
        self.assertTrue(schema.tables["public.bank_account"].columns["id"].auto_increment)
        self.assertEqual(3, sum(constraint.kind == "check" for constraint in schema.constraints.values()))

    def test_function_body_semicolons_do_not_split_schema(self) -> None:
        schema = parse_schema(
            """
            CREATE OR REPLACE FUNCTION public.touch() RETURNS trigger
            LANGUAGE plpgsql AS $$ BEGIN NEW.value = 1; RETURN NEW; END; $$;
            CREATE TABLE public.example (id bigint NOT NULL);
            CREATE TRIGGER example_touch BEFORE UPDATE ON public.example
            FOR EACH ROW EXECUTE FUNCTION public.touch();
            """,
            "postgres",
        )
        self.assertEqual(["public.example"], sorted(schema.tables))
        self.assertIn("public.touch()", schema.functions)
        self.assertIn("public.example.example_touch", schema.triggers)

    def test_check_constraints_are_parsed(self) -> None:
        schema = parse_schema(
            """
            CREATE TABLE public.example (value integer NOT NULL);
            ALTER TABLE public.example
                ADD CONSTRAINT example_value_check CHECK (value >= 0);
            """,
            "postgres",
        )
        constraint = schema.constraints["public.example.example_value_check"]
        self.assertEqual("check", constraint.kind)
        self.assertEqual("(value >= 0)", constraint.definition)


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

    def test_missing_required_column_without_default_is_blocked(self) -> None:
        desired = parse_schema(
            "CREATE TABLE public.Example (id int NOT NULL, required text NOT NULL);",
            "postgres",
        )
        actual = parse_schema(
            "CREATE TABLE public.Example (id int NOT NULL);",
            "postgres",
        )

        diff = compare_schemas(desired, actual)
        plan = build_plan(desired, diff)

        self.assertTrue(diff.has_blocking_changes)
        self.assertTrue(plan.blocked_reasons)
        self.assertFalse(plan.statements)

    def test_generated_column_expression_is_part_of_column_signature(self) -> None:
        desired = parse_schema(
            """
            CREATE TABLE `Example` (
                `Scope` varchar(32) NOT NULL,
                `ScopeKey` int GENERATED ALWAYS AS ((case when (`Scope` = 'MEMBERSHIP') then 0 else NULL end)) STORED
            );
            """,
            "mysql",
        )
        actual = parse_schema(
            "CREATE TABLE `Example` (`Scope` varchar(32) NOT NULL, `ScopeKey` int);",
            "mysql",
        )

        diff = compare_schemas(desired, actual)

        self.assertTrue(diff.has_blocking_changes)
        self.assertTrue(any(difference.category == "column" for difference in diff.differences))
        self.assertIn("generated=", format_details(diff))

    def test_postgres_nulls_not_distinct_is_part_of_index_signature(self) -> None:
        desired = parse_schema(
            """
            CREATE TABLE public.Example (value text);
            CREATE UNIQUE INDEX example_value_key
                ON public.Example (value) NULLS NOT DISTINCT;
            """,
            "postgres",
        )
        actual = parse_schema(
            """
            CREATE TABLE public.Example (value text);
            CREATE UNIQUE INDEX example_value_key ON public.Example (value);
            """,
            "postgres",
        )

        diff = compare_schemas(desired, actual)

        self.assertTrue(diff.has_blocking_changes)
        self.assertTrue(any(difference.category == "index" for difference in diff.differences))

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

    def test_missing_check_constraint_is_planned_as_confirmed_addition(self) -> None:
        desired = parse_schema(
            """
            CREATE TABLE public.example (value integer NOT NULL);
            ALTER TABLE public.example
                ADD CONSTRAINT example_value_check CHECK (value >= 0);
            """,
            "postgres",
        )
        actual = parse_schema("CREATE TABLE public.example (value integer NOT NULL);", "postgres")

        diff = compare_schemas(desired, actual)
        plan = build_plan(desired, diff)

        self.assertIn("Missing check constraint", format_details(diff))
        check_statement = next(statement for statement in plan.statements if "check constraint" in statement.reason)
        self.assertTrue(check_statement.requires_confirmation)
        self.assertIn("ADD CONSTRAINT example_value_check CHECK", check_statement.sql)


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

    def test_postgres_functions_are_created_before_triggers(self) -> None:
        desired = parse_schema(
            """
            CREATE OR REPLACE FUNCTION public.touch() RETURNS trigger
            LANGUAGE plpgsql AS $$ BEGIN RETURN NEW; END; $$;
            CREATE TABLE public.example (id bigint NOT NULL);
            CREATE TRIGGER example_touch BEFORE UPDATE ON public.example
            FOR EACH ROW EXECUTE FUNCTION public.touch();
            """,
            "postgres",
        )
        plan = build_plan(desired, compare_schemas(desired, SchemaDefinition("postgres")))
        function_position = next(
            index for index, statement in enumerate(plan.statements) if "FUNCTION" in statement.sql
        )
        trigger_position = next(
            index for index, statement in enumerate(plan.statements) if "TRIGGER" in statement.sql
        )
        self.assertLess(function_position, trigger_position)
        self.assertTrue(plan.statements[function_position].requires_confirmation)
        self.assertTrue(plan.statements[trigger_position].requires_confirmation)

    def test_deploy_requires_explicit_confirmation(self) -> None:
        desired = _schema_with_column("new_column")
        actual = _schema_with_column()
        plan = build_plan(desired, compare_schemas(desired, actual))
        with self.assertRaises(Exception):
            execute_plan(_FakeConnection(), plan)
        self.assertEqual([], _FakeConnection().executed)

    def test_mysql_failure_reports_non_transactional_partial_deployment(self) -> None:
        plan = DeploymentPlan(
            "mysql",
            [
                PlannedStatement("CREATE TABLE first_table (id int);", "first"),
                PlannedStatement("CREATE TABLE second_table (id int);", "second"),
            ],
        )
        connection = _FailingMySqlConnection(fail_on=2)

        with self.assertRaises(DeploymentExecutionError) as context:
            execute_plan(connection, plan, confirm=True)

        self.assertEqual(["CREATE TABLE first_table (id int);"], connection.executed)
        self.assertEqual(1, connection.commit_count)
        self.assertEqual(0, connection.rollback_count)
        self.assertIn("non-transactional", str(context.exception))
        self.assertIn("after 1 of 2 statements", str(context.exception))


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


def format_details(diff: object) -> str:
    return "\n".join(
        f"{difference.detail} {difference.subject}"
        for difference in getattr(diff, "differences", [])
    )


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


class _FailingMySqlConnection:
    def __init__(self, fail_on: int) -> None:
        self.fail_on = fail_on
        self.executed: list[str] = []
        self.execute_count = 0
        self.commit_count = 0
        self.rollback_count = 0

    def cursor(self) -> "_FailingMySqlCursor":
        return _FailingMySqlCursor(self)

    def commit(self) -> None:
        self.commit_count += 1

    def rollback(self) -> None:
        self.rollback_count += 1


class _FailingMySqlCursor:
    def __init__(self, connection: _FailingMySqlConnection) -> None:
        self.connection = connection

    def execute(self, sql: str) -> None:
        self.connection.execute_count += 1
        if self.connection.execute_count == self.connection.fail_on:
            raise RuntimeError("simulated MySQL DDL failure")
        self.connection.executed.append(sql)

    def close(self) -> None:
        pass


class _FakeMySqlMetadataConnection:
    def __init__(self) -> None:
        self.queries: list[str] = []

    def cursor(self) -> "_FakeMySqlMetadataCursor":
        return _FakeMySqlMetadataCursor(self)


class _FakeMySqlMetadataCursor:
    def __init__(self, connection: _FakeMySqlMetadataConnection) -> None:
        self.connection = connection
        self.sql = ""

    def execute(self, sql: str) -> None:
        self.sql = sql
        self.connection.queries.append(sql)

    def fetchall(self) -> list[dict[str, object]]:
        if self.sql.startswith("SHOW CREATE TABLE"):
            return [
                {
                    "Table": "PaymentOffers",
                    "Create Table": (
                        "CREATE TABLE `PaymentOffers` ("
                        "`Scope` varchar(32) NOT NULL, "
                        "`ContentItemScopeKey` int GENERATED ALWAYS AS "
                        "((case when (`Scope` = _utf8mb4'MEMBERSHIP') then 0 else NULL end)) STORED"
                        ")"
                    ),
                }
            ]
        if "information_schema.tables" in self.sql:
            return [{"table_name": "PaymentOffers"}]
        if "information_schema.columns" in self.sql:
            return [
                {
                    "table_name": "PaymentOffers",
                    "column_name": "Scope",
                    "column_type": "varchar(32)",
                    "is_nullable": "NO",
                    "column_default": None,
                    "extra": "",
                    "generation_expression": None,
                },
                {
                    "table_name": "PaymentOffers",
                    "column_name": "ContentItemScopeKey",
                    "column_type": "int",
                    "is_nullable": "YES",
                    "column_default": None,
                    "extra": "STORED GENERATED",
                    "generation_expression": "(case when (`Scope` = _latin1\\'MEMBERSHIP\\') then 0 else NULL end)",
                },
            ]
        return []

    def close(self) -> None:
        pass


if __name__ == "__main__":
    unittest.main()
