"""Build and execute safe schema deployment plans."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any

from .schema_compare import SchemaDiff
from .schema_reader import SchemaDefinition, quote_identifier


class DeploymentBlocked(RuntimeError):
    """Raised when a deployment would be destructive, ambiguous or unapproved."""


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

    missing_tables = {
        difference.subject
        for difference in diff.differences
        if difference.category == "table" and difference.detail == "Missing table"
    }
    for key in sorted(missing_tables):
        table = desired.tables[key]
        plan.statements.append(
            PlannedStatement(table.create_sql, f"create missing table {key}")
        )

    for difference in diff.differences:
        if difference.category != "column" or difference.detail != "Missing column":
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
            if index.primary and f"{index.table}.{index.name}" in primary_constraints:
                continue
            plan.statements.append(
                PlannedStatement(
                    _index_sql(index, desired.database),
                    f"create missing index {difference.subject}",
                    difference.requires_confirmation,
                )
            )

    for difference in diff.differences:
        if difference.category == "constraint" and difference.detail.startswith("Missing "):
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

    return plan


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
            "Deployment contains indexes, constraints or views requiring "
            "--allow-risk. No SQL was executed."
        )

    cursor = connection.cursor()
    try:
        for statement in plan.statements:
            cursor.execute(statement.sql)
        connection.commit()
    except Exception:
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

