#!/usr/bin/env bash
set -euo pipefail

: "${MYSQL_HOST:=mysql}"
: "${MYSQL_DATABASE:?MYSQL_DATABASE is required}"
: "${MYSQL_ROOT_PASSWORD:?MYSQL_ROOT_PASSWORD is required}"
: "${FINANCE_POSTGRES_HOST:=finance-postgres}"
: "${FINANCE_POSTGRES_DB:?FINANCE_POSTGRES_DB is required}"
: "${FINANCE_POSTGRES_ADMIN_PASSWORD:?FINANCE_POSTGRES_ADMIN_PASSWORD is required}"
: "${SQL_ROOT:=/workspace/SQL}"

mysql_cmd() {
    MYSQL_PWD="$MYSQL_ROOT_PASSWORD" mariadb \
        --protocol=tcp \
        --skip-ssl \
        --host="$MYSQL_HOST" \
        --user=root \
        --database="$MYSQL_DATABASE" \
        "$@"
}

postgres_cmd() {
    PGPASSWORD="$FINANCE_POSTGRES_ADMIN_PASSWORD" psql \
        --host="$FINANCE_POSTGRES_HOST" \
        --username=postgres \
        --dbname="$FINANCE_POSTGRES_DB" \
        "$@"
}

mysql_cmd <<'SQL'
CREATE TABLE IF NOT EXISTS LocalDevelopmentSeedRuns (
    SeedName varchar(128) NOT NULL,
    AppliedAt datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (SeedName)
);
SQL

mysql_seeded="$(mysql_cmd --batch --skip-column-names --silent -e "SELECT COUNT(*) FROM LocalDevelopmentSeedRuns WHERE SeedName = 'gammasite-test-seed';")"
if [[ "$mysql_seeded" != "0" ]]; then
    echo "GamMaSite test seed is already recorded; skipping MySQL seed."
else
    mysql_existing_rows="$(mysql_cmd --batch --skip-column-names --silent -e '
        SELECT
            (SELECT COUNT(*) FROM `AspNetUserClaims`) +
            (SELECT COUNT(*) FROM `AspNetUserLogins`) +
            (SELECT COUNT(*) FROM `DeviceCodes`) +
            (SELECT COUNT(*) FROM `EventRegistrations`) +
            (SELECT COUNT(*) FROM `PaymentSubscriptions`) +
            (SELECT COUNT(*) FROM `PaymentOffers`) +
            (SELECT COUNT(*) FROM `ContentLinks`) +
            (SELECT COUNT(*) FROM `ContentItems`) +
            (SELECT COUNT(*) FROM `EmailTemplates`) +
            (SELECT COUNT(*) FROM `AspNetUserTokens`) +
            (SELECT COUNT(*) FROM `AspNetUserRoles`) +
            (SELECT COUNT(*) FROM `AspNetRoleClaims`) +
            (SELECT COUNT(*) FROM `RolePermissions`) +
            (SELECT COUNT(*) FROM `Permissions`) +
            (SELECT COUNT(*) FROM `AspNetUsers`) +
            (SELECT COUNT(*) FROM `AspNetRoles`);')"
    if [[ "$mysql_existing_rows" != "0" ]]; then
        echo "Refusing to run the GamMaSite test seed: one or more seed tables already contain data." >&2
        exit 6
    fi

    # The local schema contains tables with both MySQL 8's default utf8mb4
    # collations and utf8mb4_general_ci. Normalize them only after the safety
    # check and immediately before the seed so existing data is not rewritten.
    mysql_cmd <<'SQL'
    SET FOREIGN_KEY_CHECKS=0;
    ALTER TABLE `AspNetUsers`
        CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
    ALTER TABLE `AspNetRoles`
        CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
    ALTER TABLE `AspNetRoleClaims`
        CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
    ALTER TABLE `AspNetUserClaims`
        CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
    ALTER TABLE `AspNetUserLogins`
        CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
    ALTER TABLE `AspNetUserRoles`
        CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
    ALTER TABLE `AspNetUserTokens`
        CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
    ALTER TABLE `ContentItems`
        CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
    ALTER TABLE `ContentLinks`
        CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
    ALTER TABLE `DeviceCodes`
        CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
    ALTER TABLE `EmailTemplates`
        CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
    ALTER TABLE `EventRegistrations`
        CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
    ALTER TABLE `PaymentOffers`
        CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
    ALTER TABLE `PaymentSubscriptions`
        CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
    ALTER TABLE `Permissions`
        CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
    ALTER TABLE `RolePermissions`
        CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
    SET FOREIGN_KEY_CHECKS=1;
SQL

    mysql_cmd < "$SQL_ROOT/test-seed/00_seed_all.sql"
    mysql_cmd -e "INSERT INTO LocalDevelopmentSeedRuns (SeedName) VALUES ('gammasite-test-seed');"
    echo "GamMaSite test seed completed."
fi

postgres_cmd <<'SQL'
CREATE TABLE IF NOT EXISTS public.local_development_seed_runs (
    seed_name text PRIMARY KEY,
    applied_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);
SQL

finance_seeded="$(postgres_cmd --tuples-only --no-align -c "SELECT COUNT(*) FROM public.local_development_seed_runs WHERE seed_name = 'gamma-finance-test-seed';")"
if [[ "$finance_seeded" != "0" ]]; then
    echo "Finance test seed is already recorded; skipping PostgreSQL seed."
else
    finance_existing_rows="$(postgres_cmd --tuples-only --no-align -c '
        SELECT
            (SELECT COUNT(*) FROM public.account) +
            (SELECT COUNT(*) FROM public.postering_group) +
            (SELECT COUNT(*) FROM public.forecast) +
            (SELECT COUNT(*) FROM public.bank_account) +
            (SELECT COUNT(*) FROM public.mobilepay) +
            (SELECT COUNT(*) FROM public.posteringer) +
            (SELECT COUNT(*) FROM public.import_history);')"
    if [[ "$finance_existing_rows" != "0" ]]; then
        echo "Refusing to run the Finance test seed: one or more seed tables already contain data." >&2
        exit 7
    fi

    postgres_cmd --set ON_ERROR_STOP=1 --file="$SQL_ROOT/finance/test-seed/00_gamma_finance_seed.sql" >/dev/null
    postgres_cmd --set ON_ERROR_STOP=1 --command "INSERT INTO public.local_development_seed_runs (seed_name) VALUES ('gamma-finance-test-seed');" >/dev/null
    echo "Finance test seed completed."
fi
