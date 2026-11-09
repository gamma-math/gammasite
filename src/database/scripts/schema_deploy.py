"""Build and execute safe schema deployment plans."""

from __future__ import annotations

from dataclasses import dataclass, field
import re
from typing import Any

from .schema_compare import SchemaDiff
from .schema_reader import (
    SchemaDefinition,
    find_matching_parenthesis,
    quote_identifier,
)


class DeploymentBlocked(RuntimeError):
    """Raised when a deployment would be destructive, ambiguous or unapproved."""


class DeploymentExecutionError(RuntimeError):
    """Raised when execution fails after one or more statements were attempted."""


@dataclass(frozen=True)
class PlannedStatement:
    sql: str
    reason: str
    requires_confirmation: bool = False


@dataclass
class DeploymentPlan:
    database: str
    statements: list[PlannedStatement] = field(default_factory=list)
    blocked_reasons: list[str] = field(default_factory=list)

    @property
    def requires_confirmation(self) -> bool:
        return any(statement.requires_confirmation for statement in self.statements)

    @property
    def is_empty(self) -> bool:
        return not self.statements and not self.blocked_reasons


def build_plan(desired: SchemaDefinition, diff: SchemaDiff) -> DeploymentPlan:
    """Generate additive SQL without generating DROP or destructive ALTER SQL."""

    plan = DeploymentPlan(database=desired.database)
    for difference in diff.blocking_changes:
        plan.blocked_reasons.append(
            f"{difference.category} {difference.subject}: {difference.detail}"
        )

    # PostgreSQL triggers depend on their functions. Emit routines before any
    # trigger statements, while still refusing to replace changed routines.
    for difference in diff.differences:
        if difference.category == "function" and difference.detail == "Missing function":
            function = desired.functions[difference.subject]
            plan.statements.append(
                PlannedStatement(
                    function.sql,
                    f"create missing function {difference.subject}",
                    difference.requires_confirmation,
                )
            )

    missing_tables = {
        difference.subject
        for difference in diff.differences
        if difference.category == "table" and difference.detail == "Missing table"
    }
    inlined_primary_keys: set[str] = set()
    for key in sorted(missing_tables):
        table = desired.tables[key]
        create_sql, primary_was_inlined = _create_table_sql(table, desired)
        if primary_was_inlined:
            inlined_primary_keys.update(
                constraint.name_key
                for constraint in desired.constraints.values()
                if constraint.table == key and constraint.kind == "primary"
            )
        plan.statements.append(
            PlannedStatement(create_sql, f"create missing table {key}")
        )

    for difference in diff.differences:
        if difference.category != "column" or difference.detail != "Missing column":
            continue
        if not difference.safe:
            # Keep unsafe additions visible in the blocked plan, but never
            # produce executable SQL for a required column without a default.
            continue
        table_key, column_name = difference.subject.rsplit(".", 1)
        table = desired.tables[table_key]
        column = table.columns[column_name]
        plan.statements.append(
            PlannedStatement(
                f"ALTER TABLE {_render_table(table_key, desired.database)} "
                f"ADD COLUMN {column.sql};",
                f"add missing column {difference.subject}",
            )
        )

    primary_constraints = {
        difference.subject
        for difference in diff.differences
        if difference.category == "constraint"
        and difference.detail.endswith("constraint")
        and desired.constraints.get(difference.subject, None) is not None
        and desired.constraints[difference.subject].kind == "primary"
    }
    for difference in diff.differences:
        if difference.category == "index" and difference.detail == "Missing index":
            index = desired.indexes[difference.subject]
            if difference.subject in inlined_primary_keys:
                continue
            if index.primary and f"{index.table}.{index.name}" in primary_constraints:
                continue
            plan.statements.append(
                PlannedStatement(
                    _index_sql(index, desired.database),
                    f"create missing index {difference.subject}",
                    difference.requires_confirmation,
                )
            )

    missing_constraints = [
        difference
        for difference in diff.differences
        if difference.category == "constraint" and difference.detail.startswith("Missing ")
    ]
    for difference in sorted(
        missing_constraints,
        key=lambda item: (
            0 if desired.constraints[item.subject].kind == "primary" else 1,
            item.subject,
        ),
    ):
        if difference.subject in inlined_primary_keys:
            continue
        constraint = desired.constraints[difference.subject]
        plan.statements.append(
            PlannedStatement(
                constraint.sql,
                f"add missing {constraint.kind} constraint {difference.subject}",
                difference.requires_confirmation,
            )
        )

    for difference in diff.differences:
        if difference.category == "view" and difference.detail in {"Missing view", "View definition differs"}:
            view = desired.views[difference.subject]
            plan.statements.append(
                PlannedStatement(
                    view.sql,
                    f"create/update view {difference.subject}",
                    difference.requires_confirmation,
                )
            )

    for difference in diff.differences:
        if difference.category == "trigger" and difference.detail == "Missing trigger":
            trigger = desired.triggers[difference.subject]
            plan.statements.append(
                PlannedStatement(
                    trigger.sql,
                    f"create missing trigger {difference.subject}",
                    difference.requires_confirmation,
                )
            )

    return plan


def _create_table_sql(table: Any, desired: SchemaDefinition) -> tuple[str, bool]:
    """Make new tables complete and valid at their initial CREATE step.

    MySQL requires an AUTO_INCREMENT column to be indexed when the table is
    created. The source schema stores primary keys in later ALTER TABLE
    statements, so the primary key must be moved into CREATE TABLE for a new
    table. The returned boolean identifies a primary key that must not be
    emitted a second time by the rest of the deployment plan. PostgreSQL
    identity definitions are handled similarly because they are represented as
    later ALTER statements in the source dump.
    """

    primary_inline = _table_has_inline_primary_key(table)

    if desired.database == "postgres":
        return _postgres_create_table_sql(table), primary_inline

    if desired.database != "mysql":
        return table.create_sql, primary_inline

    if not any(column.auto_increment for column in table.columns.values()):
        return table.create_sql, primary_inline

    primary = desired.indexes.get(f"{table.key}.PRIMARY")
    if primary is None or not primary.primary or not primary.columns:
        return table.create_sql, primary_inline

    create_sql = table.create_sql
    open_paren = create_sql.find("(")
    if open_paren < 0:
        return create_sql, False
    close_paren = find_matching_parenthesis(create_sql, open_paren)
    table_body = create_sql[open_paren + 1 : close_paren]

    # Keep source definitions that already contain an inline primary key
    # unchanged, while still suppressing the duplicate ALTER TABLE statement.
    if primary_inline:
        return create_sql, True

    primary_columns = ", ".join(quote_identifier(column) for column in primary.columns)
    return (
        create_sql[:close_paren].rstrip()
        + f",\n  PRIMARY KEY ({primary_columns})\n"
        + create_sql[close_paren:],
        True,
    )


def _table_has_inline_primary_key(table: Any) -> bool:
    """Return whether a table's CREATE body already declares a primary key."""

    create_sql = table.create_sql
    open_paren = create_sql.find("(")
    if open_paren < 0:
        return False
    close_paren = find_matching_parenthesis(create_sql, open_paren)
    table_body = create_sql[open_paren + 1 : close_paren]
    return re.search(r"\bPRIMARY\s+KEY\b", table_body, re.IGNORECASE) is not None


def _postgres_create_table_sql(table: Any) -> str:
    """Inline identity definitions that the source dump stores as ALTER SQL."""

    create_sql = table.create_sql
    open_paren = create_sql.find("(")
    if open_paren < 0:
        return create_sql
    close_paren = find_matching_parenthesis(create_sql, open_paren)
    table_body = create_sql[open_paren + 1 : close_paren]

    for column in table.columns.values():
        if not column.auto_increment or re.search(
            r"\bGENERATED\s+BY\s+DEFAULT\s+AS\s+IDENTITY\b",
            column.sql,
            re.IGNORECASE,
        ):
            continue
        identity_sql = re.sub(
            r"\bNOT\s+NULL\b",
            "GENERATED BY DEFAULT AS IDENTITY NOT NULL",
            column.sql,
            count=1,
            flags=re.IGNORECASE,
        )
        if identity_sql == column.sql:
            identity_sql = f"{column.sql} GENERATED BY DEFAULT AS IDENTITY"
        table_body = table_body.replace(column.sql, identity_sql, 1)

    return create_sql[:open_paren + 1] + table_body + create_sql[close_paren:]


def execute_plan(
    connection: Any,
    plan: DeploymentPlan,
    confirm: bool = False,
    allow_risky: bool = False,
) -> None:
    """Execute only an explicitly confirmed additive deployment plan."""

    if plan.blocked_reasons:
        raise DeploymentBlocked(
            "Deployment blocked:\n- " + "\n- ".join(plan.blocked_reasons)
        )
    if not plan.statements:
        return
    if not confirm:
        raise DeploymentBlocked("Deployment requires --confirm. No SQL was executed.")
    if plan.requires_confirmation and not allow_risky:
        raise DeploymentBlocked(
            "Deployment contains indexes, constraints, views, functions or triggers requiring "
            "--allow-risk. No SQL was executed."
        )

    cursor = connection.cursor()
    executed_count = 0
    try:
        for statement in plan.statements:
            cursor.execute(statement.sql)
            executed_count += 1
            if plan.database == "mysql":
                # MySQL implicitly commits most DDL. A rollback here would
                # falsely suggest that earlier CREATE/ALTER statements were
                # undone, so commit each successful statement and report the
                # applied prefix if a later statement fails.
                connection.commit()
        if plan.database != "mysql":
            connection.commit()
    except Exception as error:
        if plan.database == "mysql":
            raise DeploymentExecutionError(
                f"MySQL deployment failed after {executed_count} of "
                f"{len(plan.statements)} statements. MySQL DDL is "
                "non-transactional; earlier statements may already be "
                "committed. Run validate before retrying."
            ) from error
        connection.rollback()
        raise
    finally:
        cursor.close()


def format_plan(plan: DeploymentPlan) -> str:
    if plan.is_empty:
        return "Deployment plan is empty: schema already matches."
    lines = [f"Deployment plan for {plan.database}:"]
    for statement in plan.statements:
        suffix = " [confirmation required]" if statement.requires_confirmation else ""
        lines.append(f"- {statement.reason}{suffix}\n  {statement.sql}")
    for reason in plan.blocked_reasons:
        lines.append(f"- BLOCKED: {reason}")
    return "\n".join(lines)


def _index_sql(index: Any, database: str) -> str:
    if index.sql:
        return index.sql
    unique = "UNIQUE " if index.unique else ""
    columns = ", ".join(index.columns)
    table = _render_table(index.table, database)
    index_name = (
        f'"{index.name.replace(chr(34), chr(34) * 2)}"'
        if database == "postgres"
        else quote_identifier(index.name)
    )
    return f"CREATE {unique}INDEX {index_name} ON {table} ({columns});"


def _render_table(table_key: str, database: str) -> str:
    parts = table_key.split(".")
    if database == "postgres" and len(parts) == 2:
        return f'"{parts[0].replace(chr(34), chr(34) * 2)}"."{parts[1].replace(chr(34), chr(34) * 2)}"'
    return quote_identifier(parts[-1])

