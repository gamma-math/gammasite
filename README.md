# GamMaSite

GamMaSite is the website for Alumneforeningen GamMa. The current application is a hybrid ASP.NET Core site with a React/Vite frontend served from the backend.

The active user experience is primarily React-based on canonical public routes without a `/react` prefix, while ASP.NET Core still owns hosting, Identity, API endpoints, payments, email, and selected security-sensitive Razor flows. Legacy `/react/*` URLs permanently redirect to their canonical equivalents.

## Technology

* ASP.NET Core on .NET 10, including ASP.NET Core Identity and Entity Framework Core
* React 19, Vite 7, Node.js 22 and npm for the frontend build and browser tests
* MySQL 8.0+ for the primary application database
* PostgreSQL for the Finance database, accessed by the backend through Npgsql and by the schema tools through psycopg
* Python 3.13 for the database schema CLI and the standalone Stripe/MobilePay helper services; PyMySQL, psycopg, requests, python-dotenv and pytest are used by those tools
* Docker Engine or Docker Desktop with Docker Compose v2 for local application/database services, test containers, seed data and webhook integrations
* Stripe, MobilePay/Vipps, Mailgun HTTP API and Google Calendar integrations; Ical.Net handles calendar data in the backend
* GitHub Actions for CI/CD and GitHub Pages for the published technical documentation
* xUnit, Moq, EF Core InMemory and Node's test runner for automated tests

## Repository Structure

Only tracked project folders/files are listed here:

```text
.
|-- .github/
|   `-- workflows/                 GitHub Actions build/deployment workflows.
|-- .vscode/                       Example editor launch/task configuration.
|-- docs/                          Project documentation and protected design references.
|   `-- github-pages/              Primary consolidated documentation site.
|-- src/
|   |-- database/                  Database schemas, schema tooling, and test seeds.
|   |-- backend/GamMaSite/         ASP.NET Core application and deployed web assets.
|   |-- frontend/                  React/Vite source application.
|   |-- services/
|   |   |-- database/               Python schema-management service and database drivers.
|   |   |-- local-development/      Docker Compose stack, seed scripts and local integrations.
|   |   |-- stripe/                 Standalone Stripe API inspection helper.
|   |   `-- vipps/                  MobilePay/Vipps CLI, Docker setup and pytest tests.
|   `-- test/
|       |-- database/               Python unit tests for schema parsing, comparison and deployment safety.
|       |-- GamMaSite.Tests/        Backend unit tests for APIs, services, Identity and Finance validation.
|       `-- frontend/               Node-based frontend tests.
|-- GamMaSite.sln                  Solution file for the backend project.
|-- global.json                    .NET SDK version pin.
|-- Makefile                       Common local build/run commands.
`-- qrcode-1.5.4.tgz               Local QR code package used by the frontend/tooling.
```

See `src/backend/GamMaSite/README.md`, `src/frontend/README.md`, and `src/database/README.md` for the core application and schema. The local Docker workflow is documented in `src/services/local-development/README.md`; the database service, Stripe helper and MobilePay/Vipps helper have their own READMEs under `src/services/`.

The consolidated technical documentation is published from `docs/github-pages/site/`. It covers administration, finance, system pages, repository structure, access control, and the React API. The design reference under `docs/muchup/` is kept separately and is not part of the published technical documentation.

## Tests

The core automated tests are located in `src/test/GamMaSite.Tests`, `src/test/frontend` and `src/test/database`. Separate mocked HTTP tests for the standalone Stripe and MobilePay/Vipps helper services are located under `src/services/*/test`. The unit tests do not require the local MySQL database, Finance PostgreSQL database, SMTP, Stripe, or external calendar services.

Run the complete test project from the repository root:

```powershell
dotnet test src/test/GamMaSite.Tests/GamMaSite.Tests.csproj
```

Run the tests without restoring packages:

```powershell
dotnet test src/test/GamMaSite.Tests/GamMaSite.Tests.csproj --no-restore
```

Run one test class or a matching test name:

```powershell
dotnet test src/test/GamMaSite.Tests/GamMaSite.Tests.csproj --filter FullyQualifiedName~FinanceImportServiceTests
dotnet test src/test/GamMaSite.Tests/GamMaSite.Tests.csproj --filter FullyQualifiedName~CreatePosting_RejectsInvalidAmount
```

Run the Python helper-service tests with their service-specific virtual environments:

```powershell
& .\src\services\stripe\.venv\Scripts\python.exe -m pytest src/services/stripe/test
& .\src\services\vipps\.venv\Scripts\python.exe -m pytest src/services/vipps/test
```

The Finance unit tests intentionally stop before opening a real PostgreSQL connection. PostgreSQL upserts, import-history persistence, bank/MobilePay matching, database roles, and full HTTP authorization require separate integration tests.

## Branching Workflow

* `master` is the production and default branch.
* `staging` is the test and integration branch before promotion to `master`.
* Create new features in `feature/*` branches and merge into `staging` via Pull Requests.
* Create bug fixes in `fix/*` branches and merge into `staging` via Pull Requests.
* Promote tested changes from `staging` to `master` via Pull Requests.
* Direct pushes to `master` and `staging` are not allowed.

## Deployment

Deployments are run manually from GitHub Actions:

1. Open the repository on GitHub.
2. Go to `Actions`.
3. Select the `Build and deploy` workflow.
4. Click `Run workflow`.
5. Choose the target environment, normally `TEST` first and `PROD` after validation.
6. Keep `Log level` as `warning` unless a more detailed workflow run is needed.
7. Start the workflow and verify the deployed site when it completes.

## Getting Started

### Prerequisites

* .NET 10 SDK
* Node.js compatible with the frontend package lock
* MySQL 8.0+
* Docker Engine or Docker Desktop with Docker Compose v2 (for the full local stack)

### Local Docker development

The Compose setup runs the React/Vite build, ASP.NET Core app, MySQL 8 and Finance PostgreSQL together. It is the recommended way to run the complete local stack:

```powershell
docker compose -f src/services/local-development/compose.yml config --quiet
docker compose -f src/services/local-development/compose.yml up -d --build
docker compose -f src/services/local-development/compose.yml ps
docker compose -f src/services/local-development/compose.yml --profile tools run --rm --build tools scripts/validate.sh
```

The app is available at `http://localhost:5000`, MySQL at `localhost:3306`, and Finance PostgreSQL at `localhost:5433`. Optional local overrides can be placed in `src/services/local-development/.env`; start from `.env.example` and do not add production secrets. The seed profile is available with `docker compose -f src/services/local-development/compose.yml --profile tools run --rm --build seed`.

Stop the stack with:

```powershell
docker compose -f src/services/local-development/compose.yml down
```

Do not add `-v` unless local database and media volumes may be deleted. See [`src/services/local-development/README.md`](src/services/local-development/README.md) for service details, troubleshooting and integration profiles.

### Build Frontend

```bash
cd src/frontend
npm install
npm run build
```

The Vite build writes production assets to `src/backend/GamMaSite/wwwroot/react-assets/`.

### Build Backend

The normal build writes the backend to `src/backend/GamMaSite/bin/Debug/net10.0/`.
Stop a running backend first, otherwise Windows may lock `GamMaSite.exe`:

```powershell
Get-Process -Name GamMaSite -ErrorAction SilentlyContinue |
  Stop-Process -Force

dotnet build src/backend/GamMaSite/GamMaSite.csproj
```

To verify the build while the local site is still running, use a separate temporary
output folder. In PowerShell, use `$env:TEMP` (not `%TEMP%`):

```powershell
dotnet build src/backend/GamMaSite/GamMaSite.csproj `
  --no-restore `
  -o "$env:TEMP\GamMaSiteBuildVerify"
```

### Run Locally

```bash
make run
```

The application starts on `https://localhost:5001` and `http://localhost:5000`.

You can also run the backend directly:

```bash
dotnet run --project src/backend/GamMaSite/GamMaSite.csproj
```

## Notes

* The React shell is served by ASP.NET Core and uses API endpoints in `src/backend/GamMaSite/Controllers/Api*.cs`.
* Some old MVC controllers/views are retained temporarily and marked with `LEGACY MVC` comments.
* Identity Razor pages are still used for selected account/security flows, especially email confirmation and authenticator setup/reset.
