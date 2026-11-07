param(
    [switch]$SkipStart
)

$ErrorActionPreference = "Stop"

$serviceRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\")).Path
$repoRoot = (Resolve-Path (Join-Path $serviceRoot "..\..\..")).Path
$envFile = Join-Path $serviceRoot ".env"
$exampleEnvFile = Join-Path $serviceRoot ".env.example"

if (-not (Test-Path -LiteralPath $envFile)) {
    $envFile = $exampleEnvFile
}

if (-not (Test-Path -LiteralPath $envFile)) {
    throw "Missing local environment file: $envFile"
}

$settings = @{}
Get-Content -LiteralPath $envFile | ForEach-Object {
    if ($_ -match '^\s*([A-Za-z_][A-Za-z0-9_]*)=(.*)$') {
        $value = $matches[2].Trim()
        if ($value.Length -ge 2 -and
            (($value.StartsWith('"') -and $value.EndsWith('"')) -or
             ($value.StartsWith("'") -and $value.EndsWith("'")))) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        $settings[$matches[1]] = $value
    }
}

function Get-Setting([string]$Name, [string]$DefaultValue) {
    if ($settings.ContainsKey($Name) -and -not [string]::IsNullOrWhiteSpace($settings[$Name])) {
        return $settings[$Name]
    }
    return $DefaultValue
}

$composeFile = Join-Path $serviceRoot "compose.yml"
if (-not $SkipStart) {
    & docker compose --env-file $envFile -f $composeFile up -d mysql finance-postgres
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

$python = Join-Path $repoRoot "src\services\database\.venv\Scripts\python.exe"
if (-not (Test-Path -LiteralPath $python)) {
    throw "Database service virtual environment is missing. Run: py -m venv src/services/database/.venv"
}

$mysqlPort = Get-Setting "MYSQL_PORT" "3306"
$mysqlDatabase = Get-Setting "MYSQL_DATABASE" "gammasite_local"
$mysqlUser = Get-Setting "MYSQL_APP_USER" "gammasite_app"
$mysqlPassword = Get-Setting "MYSQL_APP_PASSWORD" "change-me-local"
$postgresPort = Get-Setting "FINANCE_POSTGRES_PORT" "5433"
$postgresDatabase = Get-Setting "FINANCE_POSTGRES_DB" "gamma_finance_local"
$postgresPassword = Get-Setting "FINANCE_WRITE_PASSWORD" "change-me-local-finance-write"

$env:DBMANAGER_MYSQL_LOCAL_CONNECTION_STRING = `
    "Server=127.0.0.1;Port=$mysqlPort;Database=$mysqlDatabase;User=$mysqlUser;Password=$mysqlPassword;"
$env:DBMANAGER_POSTGRES_LOCAL_CONNECTION_STRING = `
    "Host=127.0.0.1;Port=$postgresPort;Database=$postgresDatabase;Username=gamma_finance_write;Password=$postgresPassword;"

& $python -m src.services.database validate --db mysql --env local --connection-env DBMANAGER_MYSQL_LOCAL_CONNECTION_STRING
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& $python -m src.services.database validate --db postgres --env local --connection-env DBMANAGER_POSTGRES_LOCAL_CONNECTION_STRING
exit $LASTEXITCODE
