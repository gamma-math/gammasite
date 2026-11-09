"""Compare desired schema definitions with live database metadata."""

from __future__ import annotations

from dataclasses import dataclass, field
import re
from typing import Any

from .schema_reader import (
    ColumnDefinition,
    ConstraintDefinition,
    FunctionDefinition,
    IndexDefinition,
    SchemaDefinition,
    TableDefinition,
    TriggerDefinition,
    ViewDefinition,
    normalize_sql,
)


@dataclass(frozen=True)
class SchemaDifference:
    """One reported difference and its deployment safety classification."""

    category: str
    subject: str
    detail: str
    safe: bool
    requires_confirmation: bool = False

    @property
    def blocking(self) -> bool:
        return not self.safe


@dataclass
class SchemaDiff:
    """Complete comparison result for one database."""

    database: str
    differences: list[SchemaDifference] = field(default_factory=list)

    @property
    def is_match(self) -> bool:
        return not self.differences

    @property
    def has_blocking_changes(self) -> bool:
        return any(difference.blocking for difference in self.differences)

    @property
    def has_confirmation_changes(self) -> bool:
        return any(difference.requires_confirmation for difference in self.differences)

    @property
    def safe_changes(self) -> list[SchemaDifference]:
        return [difference for difference in self.differences if difference.safe]

    @property
    def blocking_changes(self) -> list[SchemaDifference]:
        return [difference for difference in self.differences if difference.blocking]


def compare_schemas(desired: SchemaDefinition, actual: SchemaDefinition) -> SchemaDiff:
    """Compare tables, columns, indexes, constraints, views and PostgreSQL routines.

    Extra objects and incompatible changes are reported but are never treated
    as safe deployment operations. This is the central safety boundary used by
    both validation and deployment planning.
    """

    if desired.database != actual.database:
        raise ValueError("Cannot compare schemas from different database engines.")

    result = SchemaDiff(database=desired.database)
    desired_tables = desired.tables
    actual_tables = actual.tables

    for key in sorted(desired_tables.keys() - actual_tables.keys()):
        table = desired_tables[key]
        result.differences.append(
            SchemaDifference("table", key, "Missing table", safe=True)
        )

    for key in sorted(actual_tables.keys() - desired_tables.keys()):
        result.differences.append(
            SchemaDifference("table", key, "Extra table exists in database", safe=False)
        )

    for key in sorted(desired_tables.keys() & actual_tables.keys()):
        _compare_columns(result, desired_tables[key], actual_tables[key])

    _compare_indexes(result, desired, actual)
    _compare_constraints(result, desired, actual)
    _compare_views(result, desired, actual)
    _compare_functions(result, desired, actual)
    _compare_triggers(result, desired, actual)
    return result


def format_diff(diff: SchemaDiff) -> str:
    """Render a concise human-readable report."""

    if diff.is_match:
        return f"Schema valid: {diff.database} matches the local definition."

    lines = [f"Schema differences for {diff.database}:"]
    for difference in diff.differences:
        safety = "safe" if difference.safe else "BLOCKED"
        confirmation = "; confirmation required" if difference.requires_confirmation else ""
        lines.append(
            f"- [{safety}{confirmation}] {difference.category} {difference.subject}: {difference.detail}"
        )
    return "\n".join(lines)


def _compare_columns(result: SchemaDiff, desired: TableDefinition, actual: TableDefinition) -> None:
    for name in sorted(desired.columns.keys() - actual.columns.keys()):
        column = desired.columns[name]
        # Adding a nullable column or a column with a default preserves existing
        # rows. A required column without a default cannot be added safely when
        # the table already contains data, so deployment must stop for review.
        safe_addition = column.nullable or column.default is not None
        result.differences.append(
            SchemaDifference(
                "column",
                f"{desired.key}.{name}",
                "Missing column",
                safe=safe_addition,
            )
        )
    for name in sorted(actual.columns.keys() - desired.columns.keys()):
        result.differences.append(
            SchemaDifference(
                "column",
                f"{actual.key}.{name}",
                "Extra column exists in database",
                safe=False,
            )
        )
    for name in sorted(desired.columns.keys() & actual.columns.keys()):
        expected = desired.columns[name]
        current = actual.columns[name]
        if expected.signature != current.signature:
            result.differences.append(
                SchemaDifference(
                    "column",
                    f"{desired.key}.{name}",
                    _column_change_detail(expected, current),
                    safe=False,
                )
            )


def _compare_indexes(result: SchemaDiff, desired: SchemaDefinition, actual: SchemaDefinition) -> None:
    for key in sorted(desired.indexes.keys() - actual.indexes.keys()):
        index = desired.indexes[key]
        result.differences.append(
            SchemaDifference(
                "index",
                key,
                "Missing index",
                safe=True,
                requires_confirmation=index.unique or index.primary,
            )
        )
    for key in sorted(actual.indexes.keys() - desired.indexes.keys()):
        result.differences.append(
            SchemaDifference("index", key, "Extra index exists in database", safe=False)
        )
    for key in sorted(desired.indexes.keys() & actual.indexes.keys()):
        expected = desired.indexes[key]
        current = actual.indexes[key]
        if expected.signature != current.signature:
            result.differences.append(
                SchemaDifference(
                    "index",
                    key,
                    "Index definition differs",
                    safe=False,
                )
            )


def _compare_constraints(result: SchemaDiff, desired: SchemaDefinition, actual: SchemaDefinition) -> None:
    for key in sorted(desired.constraints.keys() - actual.constraints.keys()):
        constraint = desired.constraints[key]
        result.differences.append(
            SchemaDifference(
                "constraint",
                key,
                f"Missing {constraint.kind} constraint",
                safe=True,
                requires_confirmation=True,
            )
        )
    for key in sorted(actual.constraints.keys() - desired.constraints.keys()):
        result.differences.append(
            SchemaDifference("constraint", key, "Extra constraint exists in database", safe=False)
        )
    for key in sorted(desired.constraints.keys() & actual.constraints.keys()):
        expected = desired.constraints[key]
        current = actual.constraints[key]
        if expected.kind != current.kind or _normalize_constraint_definition(
            expected.definition,
            desired.database,
        ) != _normalize_constraint_definition(current.definition, desired.database):
            result.differences.append(
                SchemaDifference(
                    "constraint",
                    key,
                    "Constraint definition differs",
                    safe=False,
                )
            )


def _compare_views(result: SchemaDiff, desired: SchemaDefinition, actual: SchemaDefinition) -> None:
    for key in sorted(desired.views.keys() - actual.views.keys()):
        result.differences.append(
            SchemaDifference("view", key, "Missing view", safe=True, requires_confirmation=True)
        )
    for key in sorted(actual.views.keys() - desired.views.keys()):
        result.differences.append(
            SchemaDifference("view", key, "Extra view exists in database", safe=False)
        )
    for key in sorted(desired.views.keys() & actual.views.keys()):
        expected = desired.views[key]
        current = actual.views[key]
        if _normalize_view_definition(expected.definition, desired.database) != _normalize_view_definition(
            current.definition,
            desired.database,
        ):
            result.differences.append(
                SchemaDifference(
                    "view",
                    key,
                    "View definition differs",
                    safe=True,
                    requires_confirmation=True,
                )
            )


def _compare_functions(result: SchemaDiff, desired: SchemaDefinition, actual: SchemaDefinition) -> None:
    for key in sorted(desired.functions.keys() - actual.functions.keys()):
        result.differences.append(
            SchemaDifference(
                "function",
                key,
                "Missing function",
                safe=True,
                requires_confirmation=True,
            )
        )
    for key in sorted(actual.functions.keys() - desired.functions.keys()):
        result.differences.append(
            SchemaDifference("function", key, "Extra function exists in database", safe=False)
        )
    for key in sorted(desired.functions.keys() & actual.functions.keys()):
        expected = desired.functions[key]
        current = actual.functions[key]
        if _normalize_function_definition(expected, desired.database) != _normalize_function_definition(
            current,
            desired.database,
        ):
            result.differences.append(
                SchemaDifference(
                    "function",
                    key,
                    "Function definition differs",
                    safe=False,
                )
            )


def _compare_triggers(result: SchemaDiff, desired: SchemaDefinition, actual: SchemaDefinition) -> None:
    for key in sorted(desired.triggers.keys() - actual.triggers.keys()):
        result.differences.append(
            SchemaDifference(
                "trigger",
                key,
                "Missing trigger",
                safe=True,
                requires_confirmation=True,
            )
        )
    for key in sorted(actual.triggers.keys() - desired.triggers.keys()):
        result.differences.append(
            SchemaDifference("trigger", key, "Extra trigger exists in database", safe=False)
        )
    for key in sorted(desired.triggers.keys() & actual.triggers.keys()):
        expected = desired.triggers[key]
        current = actual.triggers[key]
        if _normalize_trigger_definition(expected, desired.database) != _normalize_trigger_definition(
            current,
            desired.database,
        ):
            result.differences.append(
                SchemaDifference(
                    "trigger",
                    key,
                    "Trigger definition differs",
                    safe=False,
                )
            )


def _column_change_detail(expected: ColumnDefinition, current: ColumnDefinition) -> str:
    return (
        f"expected type={expected.data_type}, nullable={expected.nullable}, "
        f"default={expected.default!r}, auto_increment={expected.auto_increment}, "
        f"generated={expected.generation_expression!r}; "
        f"actual type={current.data_type}, nullable={current.nullable}, "
        f"default={current.default!r}, auto_increment={current.auto_increment}, "
        f"generated={current.generation_expression!r}"
    )


def _normalize_constraint_definition(definition: str, database: str) -> str:
    """Normalize metadata formatting without hiding structural differences."""

    normalized = normalize_sql(definition)
    if database == "mysql":
        # MySQL dump files may keep an index prefix length in a primary key,
        # while information_schema reports only the underlying column.
        normalized = re.sub(r"\b([a-z_][a-z0-9_]*)\(\d+\)", r"\1", normalized)
    if database == "postgres":
        # pg_get_constraintdef omits the public schema for same-schema refs.
        normalized = normalized.replace("public.", "")
        # pg_get_constraintdef prefixes CHECK expressions with CHECK, while
        # the dump parser stores only the expression body.
        normalized = re.sub(r"^check\s+", "", normalized)
    return normalized


def _normalize_view_definition(definition: str, database: str) -> str:
    """Normalize harmless database qualification added by metadata providers."""

    normalized = normalize_sql(definition)
    if database == "mysql":
        # information_schema.views may qualify every column as database.table,
        # while the source definition intentionally uses the current database.
        normalized = re.sub(
            r"\b[a-z_][a-z0-9_]*\.([a-z_][a-z0-9_]*)\.([a-z_][a-z0-9_]*)\b",
            r"\1.\2",
            normalized,
        )
        normalized = re.sub(
            r"\b(from|join)\s+[a-z_][a-z0-9_]*\.([a-z_][a-z0-9_]*)\b",
            r"\1 \2",
            normalized,
        )
    return normalized


def _normalize_function_definition(function: FunctionDefinition, database: str) -> str:
    """Normalize PostgreSQL function output from source SQL and pg_get_functiondef."""

    normalized = normalize_sql(function.definition)
    # PostgreSQL may choose a generated dollar-quote tag such as $function$
    # when rendering metadata, while the source definition commonly uses $$.
    normalized = re.sub(r"\$[a-z_][a-z0-9_]*\$", "$$", normalized)
    normalized = re.sub(
        r"^create\s+or\s+replace\s+function\b",
        "create function",
        normalized,
    )
    if database == "postgres":
        normalized = normalized.replace('"public".', "public.")
    return normalized


def _normalize_trigger_definition(trigger: TriggerDefinition, database: str) -> str:
    """Normalize PostgreSQL trigger output without hiding trigger semantics."""

    normalized = normalize_sql(trigger.definition)
    if database == "postgres":
        normalized = normalized.replace('"public".', "public.")
        # pg_get_triggerdef may omit the public schema for a same-schema
        # trigger function. Canonicalize only unqualified function calls.
        normalized = re.sub(
            r"(\bexecute\s+(?:function|procedure)\s+)(?![a-z_][a-z0-9_$]*\.)",
            r"\1public.",
            normalized,
        )
    return normalized

