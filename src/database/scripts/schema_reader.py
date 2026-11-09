"""Parse local SQL definitions and read comparable database metadata.

This is intentionally a focused schema reader rather than a general SQL
parser. It understands the DDL emitted by the repository's MySQL and
PostgreSQL source files and reports constructs it cannot safely interpret.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path
import re
from typing import Any, Iterable



@dataclass(frozen=True)
class ColumnDefinition:
    name: str
    data_type: str
    nullable: bool
    default: str | None
    auto_increment: bool
    sql: str

    @property
    def signature(self) -> tuple[Any, ...]:
        return (
            self.data_type,
            self.nullable,
            None if self.auto_increment else normalize_value(self.default),
            self.auto_increment,
        )


@dataclass
class TableDefinition:
    name: str
    schema: str | None
    create_sql: str
    columns: dict[str, ColumnDefinition] = field(default_factory=dict)

    @property
    def key(self) -> str:
        return table_key(self.schema, self.name)


@dataclass(frozen=True)
class IndexDefinition:
    name: str
    table: str
    columns: tuple[str, ...]
    unique: bool = False
    primary: bool = False
    where: str | None = None
    sql: str = ""

    @property
    def signature(self) -> tuple[Any, ...]:
        return (
            self.table,
            tuple(normalize_sql(column) for column in self.columns),
            self.unique,
            self.primary,
            normalize_sql(self.where or ""),
        )

    @property
    def name_key(self) -> str:
        return f"{self.table}.{self.name}"


@dataclass(frozen=True)
class ConstraintDefinition:
    name: str
    table: str
    kind: str
    definition: str
    sql: str = ""

    @property
    def signature(self) -> tuple[str, str, str]:
        return self.table, self.kind, normalize_sql(self.definition)

    @property
    def name_key(self) -> str:
        return f"{self.table}.{self.name}"


@dataclass(frozen=True)
class ViewDefinition:
    name: str
    schema: str | None
    definition: str
    sql: str

    @property
    def key(self) -> str:
        return table_key(self.schema, self.name)


@dataclass(frozen=True)
class FunctionDefinition:
    name: str
    schema: str | None
    arguments: str
    definition: str
    sql: str = ""

    @property
    def key(self) -> str:
        return f"{table_key(self.schema, self.name)}({normalize_sql(self.arguments)})"


@dataclass(frozen=True)
class TriggerDefinition:
    name: str
    schema: str | None
    table: str
    definition: str
    sql: str = ""

    @property
    def key(self) -> str:
        return f"{self.table}.{self.name}"


@dataclass
class SchemaDefinition:
    database: str
    tables: dict[str, TableDefinition] = field(default_factory=dict)
    indexes: dict[str, IndexDefinition] = field(default_factory=dict)
    constraints: dict[str, ConstraintDefinition] = field(default_factory=dict)
    views: dict[str, ViewDefinition] = field(default_factory=dict)
    functions: dict[str, FunctionDefinition] = field(default_factory=dict)
    triggers: dict[str, TriggerDefinition] = field(default_factory=dict)


def read_schema_file(path: str | Path, database: str) -> SchemaDefinition:
    """Read and parse a local schema definition."""

    sql = Path(path).read_text(encoding="utf-8-sig")
    return parse_schema(sql, database)


def parse_schema(sql: str, database: str) -> SchemaDefinition:
    """Parse the supported schema objects from SQL text."""

    database = database.lower()
    if database not in {"mysql", "postgres"}:
        raise ValueError(f"Unsupported database: {database}")

    schema = SchemaDefinition(database=database)
    for statement in split_sql_statements(sql):
        normalized = statement.strip()
        if not normalized:
            continue

        if database == "postgres":
            function = _parse_function(normalized, database)
            if function:
                schema.functions[function.key] = function
                continue

            trigger = _parse_trigger(normalized, database)
            if trigger:
                schema.triggers[trigger.key] = trigger
                continue

        table_match = _CREATE_TABLE_RE.match(normalized)
        if table_match:
            table_schema, table_name = split_table_name(table_match.group("name"), database)
            open_paren = normalized.find("(", table_match.start("name"))
            close_paren = find_matching_parenthesis(normalized, open_paren)
            table = TableDefinition(
                name=table_name,
                schema=table_schema,
                create_sql=normalized + ";",
            )
            for entry in split_top_level(normalized[open_paren + 1 : close_paren], ","):
                _parse_table_entry(entry, table, schema)
            schema.tables[table.key] = table
            continue

        view = _parse_view(normalized, database)
        if view:
            schema.views[view.key] = view
            continue

        index = _parse_create_index(normalized, database)
        if index:
            schema.indexes[index.name_key] = index
            continue

        if re.match(r"^ALTER\s+TABLE\b", normalized, re.IGNORECASE):
            _parse_alter_table(normalized, schema, database)

    return schema


def read_database_schema(connection: Any, database: str) -> SchemaDefinition:
    """Read structural metadata from a live database connection."""

    database = database.lower()
    if database == "mysql":
        return _read_mysql_schema(connection)
    if database == "postgres":
        return _read_postgres_schema(connection)
    raise ValueError(f"Unsupported database: {database}")


def query_rows(connection: Any, sql: str) -> list[dict[str, Any]]:
    """Execute a fixed metadata query and return rows as dictionaries."""

    cursor = connection.cursor()
    try:
        cursor.execute(sql)
        rows = cursor.fetchall()
        if not rows:
            return []
        if isinstance(rows[0], dict):
            return [dict(row) for row in rows]
        names = [description[0] for description in cursor.description]
        return [dict(zip(names, row)) for row in rows]
    finally:
        cursor.close()


def table_key(schema: str | None, name: str) -> str:
    return f"{schema}.{name}" if schema else name


def normalize_sql(value: str) -> str:
    """Normalize harmless SQL formatting differences for comparisons."""

    normalized = (value or "").strip().rstrip(";")
    # MySQL metadata omits dump-file backticks from identifiers.
    normalized = re.sub(r"`([^`]*)`", r"\1", normalized)
    normalized = re.sub(r"\s*,\s*", ",", normalized)
    return re.sub(r"\s+", " ", normalized).lower()


def normalize_value(value: Any) -> str | None:
    if value is None:
        return None
    text = str(value).strip()
    # PostgreSQL metadata may add an implicit type cast to string defaults,
    # while the source definition leaves the same literal uncast.
    text = re.sub(
        r"('(?:''|[^'])*')::[a-z_][a-z0-9_]*(?:\s+[a-z_][a-z0-9_]*)?(?:\[\])?",
        r"\1",
        text,
        flags=re.IGNORECASE,
    )
    if len(text) >= 2 and text[0] == text[-1] and text[0] in {"'", '"'}:
        text = text[1:-1]
    return normalize_sql(text)


def split_sql_statements(sql: str) -> list[str]:
    """Split SQL while respecting quotes, dollar-quoted functions and comments."""

    source = _strip_comments(sql)
    statements: list[str] = []
    start = 0
    index = 0
    quote: str | None = None
    dollar_quote: str | None = None
    while index < len(source):
        char = source[index]
        if dollar_quote:
            if source.startswith(dollar_quote, index):
                index += len(dollar_quote)
                dollar_quote = None
                continue
            index += 1
            continue
        if quote:
            if char == quote:
                if index + 1 < len(source) and source[index + 1] == quote:
                    index += 2
                    continue
                quote = None
            elif char == "\\" and quote == "'":
                index += 2
                continue
            index += 1
            continue
        if char in {"'", '"', "`"}:
            quote = char
            index += 1
            continue
        if char == "$":
            match = re.match(r"\$[A-Za-z_0-9]*\$", source[index:])
            if match:
                dollar_quote = match.group(0)
                index += len(dollar_quote)
                continue
        if char == ";":
            statement = source[start:index].strip()
            if statement:
                statements.append(statement)
            start = index + 1
        index += 1
    tail = source[start:].strip()
    if tail:
        statements.append(tail)
    return statements


def split_top_level(value: str, separator: str = ",") -> list[str]:
    """Split comma-separated SQL fragments outside nested expressions."""

    parts: list[str] = []
    start = 0
    depth = 0
    quote: str | None = None
    for index, char in enumerate(value):
        if quote:
            if char == quote:
                quote = None
            continue
        if char in {"'", '"', "`"}:
            quote = char
        elif char == "(":
            depth += 1
        elif char == ")":
            depth -= 1
        elif char == separator and depth == 0:
            part = value[start:index].strip()
            if part:
                parts.append(part)
            start = index + 1
    tail = value[start:].strip()
    if tail:
        parts.append(tail)
    return parts


def find_matching_parenthesis(value: str, open_index: int) -> int:
    if open_index < 0 or open_index >= len(value) or value[open_index] != "(":
        raise ValueError("Expected an opening parenthesis.")
    depth = 0
    quote: str | None = None
    for index in range(open_index, len(value)):
        char = value[index]
        if quote:
            if char == quote:
                quote = None
            continue
        if char in {"'", '"', "`"}:
            quote = char
        elif char == "(":
            depth += 1
        elif char == ")":
            depth -= 1
            if depth == 0:
                return index
    raise ValueError("Unclosed SQL parenthesis.")


@dataclass(frozen=True)
class _ParsedTableName:
    schema: str | None
    name: str


_IDENTIFIER = r"(?:`[^`]+`|\"[^\"]+\"|[A-Za-z_][A-Za-z0-9_$]*)"
_TABLE_NAME = rf"{_IDENTIFIER}(?:\s*\.\s*{_IDENTIFIER})?"
_CREATE_TABLE_RE = re.compile(
    rf"^CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?(?P<name>{_TABLE_NAME})\s*\(",
    re.IGNORECASE | re.DOTALL,
)


def split_table_name(raw: str, database: str) -> tuple[str | None, str]:
    parts = [unquote_identifier(part.strip()) for part in raw.split(".")]
    if len(parts) == 1:
        return ("public" if database == "postgres" else None), parts[0]
    return parts[-2], parts[-1]


def _parse_table_entry(entry: str, table: TableDefinition, schema: SchemaDefinition) -> None:
    entry = entry.strip().rstrip(",")
    if not entry:
        return
    if re.match(r"^(CONSTRAINT|PRIMARY|UNIQUE|KEY|INDEX|FOREIGN|CHECK)\b", entry, re.IGNORECASE):
        _parse_constraint_action(
            f"ALTER TABLE {render_table_name(table.schema, table.name, schema.database)} ADD {entry}",
            schema,
            schema.database,
        )
        return

    match = re.match(rf"^(?P<name>{_IDENTIFIER})\s+(?P<rest>.+)$", entry, re.IGNORECASE | re.DOTALL)
    if not match:
        return
    name = unquote_identifier(match.group("name"))
    rest = match.group("rest").strip()
    modifier = re.search(
        r"\b(?:NOT\s+NULL|NULL|DEFAULT|GENERATED|AUTO_INCREMENT|PRIMARY|UNIQUE|REFERENCES|CHECK|COLLATE|CHARACTER\s+SET|COMMENT|ON\s+UPDATE)\b",
        rest,
        re.IGNORECASE,
    )
    type_text = rest[: modifier.start() if modifier else len(rest)].strip()
    default = _extract_default(rest)
    column = ColumnDefinition(
        name=name,
        data_type=normalize_type(type_text),
        nullable=not bool(re.search(r"\bNOT\s+NULL\b", rest, re.IGNORECASE)),
        default=default,
        auto_increment=bool(re.search(r"\b(?:AUTO_INCREMENT|GENERATED\s+BY\s+DEFAULT\s+AS\s+IDENTITY)\b", rest, re.IGNORECASE)),
        sql=entry,
    )
    table.columns[name] = column


def _parse_view(statement: str, database: str) -> ViewDefinition | None:
    match = re.match(rf"^CREATE\s+(?:OR\s+REPLACE\s+)?VIEW\s+(?P<name>{_TABLE_NAME})\s+AS\s+(?P<body>.+)$", statement, re.IGNORECASE | re.DOTALL)
    if not match:
        return None
    view_schema, view_name = split_table_name(match.group("name"), database)
    return ViewDefinition(view_name, view_schema, match.group("body").strip(), statement + ";")


def _parse_function(statement: str, database: str) -> FunctionDefinition | None:
    match = re.match(
        rf"^CREATE\s+(?:OR\s+REPLACE\s+)?FUNCTION\s+(?P<name>{_TABLE_NAME})\s*",
        statement,
        re.IGNORECASE | re.DOTALL,
    )
    if not match:
        return None
    open_paren = statement.find("(", match.end())
    if open_paren < 0:
        return None
    close_paren = find_matching_parenthesis(statement, open_paren)
    function_schema, function_name = split_table_name(match.group("name"), database)
    arguments = statement[open_paren + 1 : close_paren].strip()
    return FunctionDefinition(
        name=function_name,
        schema=function_schema,
        arguments=arguments,
        definition=statement,
        sql=statement + ";",
    )


def _parse_trigger(statement: str, database: str) -> TriggerDefinition | None:
    match = re.match(
        rf"^CREATE\s+TRIGGER\s+(?P<name>{_IDENTIFIER})\s+.*?\s+ON\s+(?P<table>{_TABLE_NAME})\s+.+$",
        statement,
        re.IGNORECASE | re.DOTALL,
    )
    if not match:
        return None
    trigger_schema, trigger_name = split_table_name(match.group("name"), database)
    table_schema, table_name = split_table_name(match.group("table"), database)
    return TriggerDefinition(
        name=unquote_identifier(trigger_name),
        schema=trigger_schema,
        table=table_key(table_schema, table_name),
        definition=statement,
        sql=statement + ";",
    )


def _parse_create_index(statement: str, database: str) -> IndexDefinition | None:
    match = re.match(
        rf"^CREATE\s+(?P<unique>UNIQUE\s+)?INDEX\s+(?:IF\s+NOT\s+EXISTS\s+)?(?P<name>{_IDENTIFIER})\s+ON\s+(?P<table>{_TABLE_NAME})(?P<tail>.*)$",
        statement,
        re.IGNORECASE | re.DOTALL,
    )
    if not match:
        return None
    table_schema, table_name = split_table_name(match.group("table"), database)
    table = table_key(table_schema, table_name)
    tail = match.group("tail")
    open_paren = tail.find("(")
    if open_paren < 0:
        return None
    close_paren = find_matching_parenthesis(tail, open_paren)
    columns = tuple(_clean_index_column(column) for column in split_top_level(tail[open_paren + 1 : close_paren]))
    where_match = re.search(r"\bWHERE\s+(.+)$", tail[close_paren + 1 :], re.IGNORECASE | re.DOTALL)
    return IndexDefinition(
        name=unquote_identifier(match.group("name")),
        table=table,
        columns=columns,
        unique=bool(match.group("unique")),
        primary=False,
        where=where_match.group(1).strip() if where_match else None,
        sql=statement + ";",
    )


def _parse_alter_table(statement: str, schema: SchemaDefinition, database: str) -> None:
    match = re.match(rf"^ALTER\s+TABLE\s+(?:ONLY\s+)?(?P<table>{_TABLE_NAME})\s+(?P<actions>.+)$", statement, re.IGNORECASE | re.DOTALL)
    if not match:
        return
    table_schema, table_name = split_table_name(match.group("table"), database)
    table = table_key(table_schema, table_name)
    identity = re.match(
        rf"^ALTER\s+COLUMN\s+(?P<column>{_IDENTIFIER})\s+ADD\s+GENERATED\s+BY\s+DEFAULT\s+AS\s+IDENTITY\b",
        match.group("actions"),
        re.IGNORECASE | re.DOTALL,
    )
    if identity:
        table_definition = schema.tables.get(table)
        column_name = unquote_identifier(identity.group("column"))
        if table_definition and column_name in table_definition.columns:
            column = table_definition.columns[column_name]
            table_definition.columns[column_name] = ColumnDefinition(
                name=column.name,
                data_type=column.data_type,
                nullable=column.nullable,
                default=column.default,
                auto_increment=True,
                sql=column.sql,
            )
        return
    for action in split_top_level(match.group("actions")):
        action = action.strip()
        if not re.match(r"^ADD\b", action, re.IGNORECASE):
            continue
        _parse_constraint_action(
            f"ALTER TABLE {match.group('table').strip()} {action}",
            schema,
            database,
            table_override=table,
        )


def _parse_constraint_action(
    statement: str,
    schema: SchemaDefinition,
    database: str,
    table_override: str | None = None,
) -> None:
    match = re.match(rf"^ALTER\s+TABLE\s+(?P<table>{_TABLE_NAME})\s+ADD\s+(?P<body>.+)$", statement, re.IGNORECASE | re.DOTALL)
    if not match:
        return
    table = table_override
    if table is None:
        table_schema, table_name = split_table_name(match.group("table"), database)
        table = table_key(table_schema, table_name)
    body = match.group("body").strip()
    constraint_name: str | None = None
    named = re.match(rf"^CONSTRAINT\s+(?P<name>{_IDENTIFIER})\s+(?P<body>.+)$", body, re.IGNORECASE | re.DOTALL)
    if named:
        constraint_name = unquote_identifier(named.group("name"))
        body = named.group("body").strip()

    primary = re.match(r"^PRIMARY\s+KEY\s*\((?P<columns>.+)\)", body, re.IGNORECASE | re.DOTALL)
    if primary:
        columns = tuple(_clean_index_column(value) for value in split_top_level(primary.group("columns")))
        # PostgreSQL assigns an inline primary key the conventional
        # ``<table>_pkey`` name. Normalize the local definition to that name
        # so it compares with the live metadata after deployment.
        index_name = constraint_name or (
            f"{table.rsplit('.', 1)[-1]}_pkey" if database == "postgres" else "PRIMARY"
        )
        index = IndexDefinition(index_name, table, columns, unique=True, primary=True, sql=statement + ";")
        schema.indexes[index.name_key] = index
        constraint = ConstraintDefinition(index_name, table, "primary", primary.group(0), statement + ";")
        schema.constraints[constraint.name_key] = constraint
        return

    foreign = re.match(r"^FOREIGN\s+KEY\s*\((?P<columns>[^)]+)\)\s+REFERENCES\s+(?P<ref>{_TABLE_NAME})\s*\((?P<refcols>[^)]+)\)(?P<tail>.*)$".replace("{_TABLE_NAME}", _TABLE_NAME), body, re.IGNORECASE | re.DOTALL)
    if foreign and constraint_name:
        constraint = ConstraintDefinition(constraint_name, table, "foreign", body, statement + ";")
        schema.constraints[constraint.name_key] = constraint
        return

    check = re.match(r"^CHECK\s+(?P<definition>\(.*\))$", body, re.IGNORECASE | re.DOTALL)
    if check and constraint_name:
        constraint = ConstraintDefinition(
            constraint_name,
            table,
            "check",
            check.group("definition"),
            statement + ";",
        )
        schema.constraints[constraint.name_key] = constraint
        return

    key = re.match(r"^(?P<unique>UNIQUE\s+)?(?:KEY|INDEX)\s+(?P<name>{_IDENTIFIER})\s*\((?P<columns>.+)\)$".replace("{_IDENTIFIER}", _IDENTIFIER), body, re.IGNORECASE | re.DOTALL)
    if key:
        columns = tuple(_clean_index_column(value) for value in split_top_level(key.group("columns")))
        index = IndexDefinition(
            name=unquote_identifier(key.group("name")),
            table=table,
            columns=columns,
            unique=bool(key.group("unique")),
            sql=statement + ";",
        )
        schema.indexes[index.name_key] = index


def _read_mysql_schema(connection: Any) -> SchemaDefinition:
    schema = SchemaDefinition(database="mysql")
    tables = query_rows(connection, """
        SELECT TABLE_NAME AS table_name
        FROM information_schema.tables
        WHERE table_schema = DATABASE()
          AND table_type = 'BASE TABLE'
          AND TABLE_NAME NOT IN ('LocalDevelopmentSeedRuns')
        ORDER BY TABLE_NAME
    """)
    columns = query_rows(connection, """
        SELECT TABLE_NAME AS table_name, COLUMN_NAME AS column_name,
               COLUMN_TYPE AS column_type, IS_NULLABLE AS is_nullable,
               COLUMN_DEFAULT AS column_default, EXTRA AS extra
        FROM information_schema.columns
        WHERE table_schema = DATABASE()
          AND TABLE_NAME NOT IN ('LocalDevelopmentSeedRuns')
        ORDER BY TABLE_NAME, ORDINAL_POSITION
    """)
    for row in tables:
        name = str(row["table_name"])
        schema.tables[name] = TableDefinition(name, None, "")
    for row in columns:
        table_name = str(row["table_name"])
        if table_name not in schema.tables:
            continue
        column = ColumnDefinition(
            name=str(row["column_name"]),
            data_type=normalize_type(str(row["column_type"])),
            nullable=str(row["is_nullable"]).upper() == "YES",
            default=None if row["column_default"] is None else str(row["column_default"]),
            auto_increment="auto_increment" in str(row["extra"]).lower(),
            sql="",
        )
        schema.tables[table_name].columns[column.name] = column

    index_rows = query_rows(connection, """
        SELECT TABLE_NAME AS table_name, INDEX_NAME AS index_name,
               NON_UNIQUE AS non_unique, SEQ_IN_INDEX AS sequence_number,
               COLUMN_NAME AS column_name
        FROM information_schema.statistics
        WHERE table_schema = DATABASE()
          AND TABLE_NAME NOT IN ('LocalDevelopmentSeedRuns')
        ORDER BY TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX
    """)
    grouped: dict[tuple[str, str], list[dict[str, Any]]] = {}
    for row in index_rows:
        grouped.setdefault((str(row["table_name"]), str(row["index_name"])), []).append(row)
    for (table_name, index_name), rows in grouped.items():
        schema.indexes[f"{table_name}.{index_name}"] = IndexDefinition(
            name=index_name,
            table=table_name,
            columns=tuple(str(row["column_name"]) for row in rows if row["column_name"] is not None),
            unique=not bool(rows[0]["non_unique"]),
            primary=index_name.upper() == "PRIMARY",
        )

    constraint_rows = query_rows(connection, """
        SELECT tc.TABLE_NAME AS table_name, tc.CONSTRAINT_NAME AS constraint_name,
               tc.CONSTRAINT_TYPE AS constraint_type,
               kcu.COLUMN_NAME AS column_name,
               kcu.REFERENCED_TABLE_NAME AS referenced_table_name,
               kcu.REFERENCED_COLUMN_NAME AS referenced_column_name,
               rc.DELETE_RULE AS delete_rule,
               rc.UPDATE_RULE AS update_rule,
               kcu.ORDINAL_POSITION AS ordinal_position
        FROM information_schema.table_constraints tc
        JOIN information_schema.key_column_usage kcu
          ON kcu.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA
         AND kcu.TABLE_NAME = tc.TABLE_NAME
         AND kcu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
        LEFT JOIN information_schema.referential_constraints rc
          ON rc.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA
         AND rc.TABLE_NAME = tc.TABLE_NAME
         AND rc.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
        WHERE tc.CONSTRAINT_SCHEMA = DATABASE()
          AND tc.CONSTRAINT_TYPE IN ('PRIMARY KEY', 'FOREIGN KEY')
          AND tc.TABLE_NAME NOT IN ('LocalDevelopmentSeedRuns')
        ORDER BY tc.TABLE_NAME, tc.CONSTRAINT_NAME, kcu.ORDINAL_POSITION
    """)
    _add_mysql_constraints(schema, constraint_rows)

    for row in query_rows(connection, """
        SELECT tc.TABLE_NAME AS table_name,
               tc.CONSTRAINT_NAME AS constraint_name,
               cc.CHECK_CLAUSE AS check_clause
        FROM information_schema.table_constraints tc
        JOIN information_schema.check_constraints cc
          ON cc.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA
         AND cc.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
        WHERE tc.CONSTRAINT_SCHEMA = DATABASE()
          AND tc.CONSTRAINT_TYPE = 'CHECK'
          AND tc.TABLE_NAME NOT IN ('LocalDevelopmentSeedRuns')
    """):
        constraint = ConstraintDefinition(
            name=str(row["constraint_name"]),
            table=str(row["table_name"]),
            kind="check",
            definition=str(row["check_clause"]),
        )
        schema.constraints[constraint.name_key] = constraint

    for row in query_rows(connection, """
        SELECT TABLE_NAME AS table_name, VIEW_DEFINITION AS view_definition
        FROM information_schema.views
        WHERE TABLE_SCHEMA = DATABASE()
    """):
        name = str(row["table_name"])
        schema.views[name] = ViewDefinition(name, None, str(row["view_definition"] or ""), "")
    return schema


def _read_postgres_schema(connection: Any) -> SchemaDefinition:
    schema = SchemaDefinition(database="postgres")
    for row in query_rows(connection, """
        SELECT table_name
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_type = 'BASE TABLE'
          AND table_name NOT IN ('local_development_seed_runs')
        ORDER BY table_name
    """):
        name = str(row["table_name"])
        schema.tables[table_key("public", name)] = TableDefinition(name, "public", "")

    for row in query_rows(connection, """
        SELECT table_name, column_name, data_type, udt_name,
               character_maximum_length, numeric_precision, numeric_scale,
               is_nullable, column_default, is_identity
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name NOT IN ('local_development_seed_runs')
        ORDER BY table_name, ordinal_position
    """):
        table = schema.tables.get(table_key("public", str(row["table_name"])))
        if table is None:
            continue
        column = ColumnDefinition(
            name=str(row["column_name"]),
            data_type=_postgres_type(row),
            nullable=str(row["is_nullable"]).upper() == "YES",
            default=None if row["column_default"] is None else str(row["column_default"]),
            auto_increment=str(row["is_identity"]).upper() == "YES",
            sql="",
        )
        table.columns[column.name] = column

    for row in query_rows(connection, """
        SELECT index_class.relname AS indexname,
               table_class.relname AS tablename,
               pg_get_indexdef(index_class.oid) AS indexdef,
               index_info.indisprimary AS is_primary
        FROM pg_index index_info
        JOIN pg_class index_class ON index_class.oid = index_info.indexrelid
        JOIN pg_class table_class ON table_class.oid = index_info.indrelid
        JOIN pg_namespace namespace_info ON namespace_info.oid = table_class.relnamespace
        WHERE namespace_info.nspname = 'public'
          AND table_class.relname NOT IN ('local_development_seed_runs')
    """):
        parsed = _parse_create_index(str(row["indexdef"]), "postgres")
        if parsed is None:
            continue
        if bool(row.get("is_primary")):
            parsed = IndexDefinition(
                name=parsed.name,
                table=parsed.table,
                columns=parsed.columns,
                unique=True,
                primary=True,
                where=parsed.where,
                sql=parsed.sql,
            )
        schema.indexes[parsed.name_key] = parsed

    for row in query_rows(connection, """
        SELECT c.relname AS table_name, con.conname AS constraint_name,
               con.contype AS constraint_type,
               pg_get_constraintdef(con.oid) AS constraint_definition
        FROM pg_constraint con
        JOIN pg_class c ON c.oid = con.conrelid
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = 'public'
          AND c.relname NOT IN ('local_development_seed_runs')
          AND con.contype IN ('p', 'f', 'c')
    """):
        kind = {
            "p": "primary",
            "f": "foreign",
            "c": "check",
        }[str(row["constraint_type"])]
        constraint = ConstraintDefinition(
            name=str(row["constraint_name"]),
            table=table_key("public", str(row["table_name"])),
            kind=kind,
            definition=str(row["constraint_definition"]),
        )
        schema.constraints[constraint.name_key] = constraint

    for row in query_rows(connection, """
        SELECT n.nspname AS schema_name,
               p.proname AS function_name,
               pg_get_function_identity_arguments(p.oid) AS arguments,
               pg_get_functiondef(p.oid) AS function_definition
        FROM pg_proc p
        JOIN pg_namespace n ON n.oid = p.pronamespace
        WHERE n.nspname = 'public'
          AND p.prokind = 'f'
    """):
        function = FunctionDefinition(
            name=str(row["function_name"]),
            schema=str(row["schema_name"]),
            arguments=str(row["arguments"] or ""),
            definition=str(row["function_definition"] or ""),
        )
        schema.functions[function.key] = function

    for row in query_rows(connection, """
        SELECT n.nspname AS schema_name,
               c.relname AS table_name,
               t.tgname AS trigger_name,
               pg_get_triggerdef(t.oid) AS trigger_definition
        FROM pg_trigger t
        JOIN pg_class c ON c.oid = t.tgrelid
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = 'public'
          AND NOT t.tgisinternal
          AND c.relname NOT IN ('local_development_seed_runs')
    """):
        trigger = TriggerDefinition(
            name=str(row["trigger_name"]),
            schema=str(row["schema_name"]),
            table=table_key("public", str(row["table_name"])),
            definition=str(row["trigger_definition"] or ""),
        )
        schema.triggers[trigger.key] = trigger

    for row in query_rows(connection, """
        SELECT table_name, view_definition
        FROM information_schema.views
        WHERE table_schema = 'public'
    """):
        name = str(row["table_name"])
        schema.views[table_key("public", name)] = ViewDefinition(
            name,
            "public",
            str(row["view_definition"] or ""),
            "",
        )
    return schema


def _add_mysql_constraints(schema: SchemaDefinition, rows: Iterable[dict[str, Any]]) -> None:
    grouped: dict[tuple[str, str, str], list[dict[str, Any]]] = {}
    for row in rows:
        key = (str(row["table_name"]), str(row["constraint_name"]), str(row["constraint_type"]))
        grouped.setdefault(key, []).append(row)
    for (table_name, constraint_name, constraint_type), values in grouped.items():
        columns = ", ".join(str(row["column_name"]) for row in values)
        if constraint_type == "PRIMARY KEY":
            definition = f"PRIMARY KEY ({columns})"
            kind = "primary"
        else:
            refs = values[0]
            ref_columns = ", ".join(str(row["referenced_column_name"]) for row in values)
            definition = (
                f"FOREIGN KEY ({columns}) REFERENCES "
                f"{refs['referenced_table_name']} ({ref_columns})"
            )
            delete_rule = str(refs.get("delete_rule") or "").upper()
            update_rule = str(refs.get("update_rule") or "").upper()
            if delete_rule and delete_rule != "NO ACTION":
                definition += f" ON DELETE {delete_rule}"
            if update_rule and update_rule != "NO ACTION":
                definition += f" ON UPDATE {update_rule}"
            kind = "foreign"
        constraint = ConstraintDefinition(constraint_name, table_name, kind, definition)
        schema.constraints[constraint.name_key] = constraint


def _postgres_type(row: dict[str, Any]) -> str:
    data_type = str(row["data_type"])
    if data_type == "character varying" and row["character_maximum_length"]:
        return normalize_type(f"varchar({row['character_maximum_length']})")
    if data_type == "numeric" and row["numeric_precision"]:
        scale = row["numeric_scale"] or 0
        return normalize_type(f"numeric({row['numeric_precision']},{scale})")
    return normalize_type(data_type)


def normalize_type(value: str) -> str:
    normalized = normalize_sql(value)
    normalized = re.sub(r"\s*,\s*", ",", normalized)
    normalized = normalized.replace("character varying", "varchar")
    normalized = normalized.replace("integer", "int")
    normalized = normalized.replace("double precision", "double")
    normalized = normalized.replace("tinyint(1)", "tinyint")
    return normalized


def render_table_name(schema: str | None, name: str, database: str = "mysql") -> str:
    if schema:
        if database == "postgres":
            return f'"{schema.replace(chr(34), chr(34) * 2)}"."{name.replace(chr(34), chr(34) * 2)}"'
        return f"{quote_identifier(schema)}.{quote_identifier(name)}"
    return quote_identifier(name)


def quote_identifier(value: str) -> str:
    return "`" + value.replace("`", "``") + "`"


def unquote_identifier(value: str) -> str:
    value = value.strip()
    if len(value) >= 2 and value[0] == value[-1] and value[0] in {"`", '"'}:
        return value[1:-1].replace(value[0] * 2, value[0])
    return value


def _extract_default(rest: str) -> str | None:
    match = re.search(r"\bDEFAULT\s+(.+)$", rest, re.IGNORECASE | re.DOTALL)
    if not match:
        return None
    value = match.group(1).strip()
    value = re.split(
        r"\s+(?:NOT\s+NULL|NULL|ON\s+UPDATE|COMMENT|COLLATE|CONSTRAINT)\b",
        value,
        maxsplit=1,
        flags=re.IGNORECASE,
    )[0].strip()
    if value.upper() == "NULL":
        return None
    return value


def _clean_index_column(value: str) -> str:
    value = value.strip()
    # MySQL dump files express an indexed text-prefix as ColumnName(255),
    # while information_schema.statistics reports the underlying column name.
    value = re.sub(r"\(\d+\)$", "", value)
    value = re.sub(r"\s+(?:ASC|DESC|NULLS\s+(?:FIRST|LAST))\b", "", value, flags=re.IGNORECASE)
    return unquote_identifier(value.strip())


def _strip_comments(sql: str) -> str:
    output: list[str] = []
    index = 0
    quote: str | None = None
    while index < len(sql):
        char = sql[index]
        if quote:
            output.append(char)
            if char == quote:
                if index + 1 < len(sql) and sql[index + 1] == quote:
                    output.append(sql[index + 1])
                    index += 2
                    continue
                quote = None
            elif char == "\\" and quote == "'" and index + 1 < len(sql):
                output.append(sql[index + 1])
                index += 2
                continue
            index += 1
            continue
        if char in {"'", '"', "`"}:
            quote = char
            output.append(char)
            index += 1
            continue
        if sql.startswith("--", index) or char == "#":
            newline = sql.find("\n", index)
            if newline < 0:
                break
            output.append("\n")
            index = newline + 1
            continue
        if sql.startswith("/*", index):
            end = sql.find("*/", index + 2)
            if end < 0:
                break
            output.append("\n" * sql[index : end + 2].count("\n"))
            index = end + 2
            continue
        output.append(char)
        index += 1
    return "".join(output)

