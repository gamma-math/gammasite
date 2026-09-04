# GamMaSite

GamMaSite is the website for Alumneforeningen GamMa. The current application is a hybrid ASP.NET Core site with a React/Vite frontend served from the backend.

The active user experience is primarily React-based under `/react/*`, while ASP.NET Core still owns hosting, Identity, API endpoints, payments, email, and selected security-sensitive Razor flows.

## Technology

* ASP.NET Core (.NET 10)
* React and Vite
* MySQL 8.0+
* ASP.NET Core Identity
* Stripe
* Ical.Net

## Repository Structure

Only tracked project folders/files are listed here:

```text
.
|-- .github/
|   `-- workflows/                 GitHub Actions build/deployment workflows.
|-- .vscode/                       Example editor launch/task configuration.
|-- docs/                          Design references, upgrade notes, and manual API test notes.
|-- SQL/                           Database schema scripts, views, and test seed data.
|-- src/
|   |-- backend/GamMaSite/         ASP.NET Core application and deployed web assets.
|   `-- frontend/                  React/Vite source application.
|-- GamMaSite.sln                  Solution file for the backend project.
|-- global.json                    .NET SDK version pin.
|-- Makefile                       Common local build/run commands.
`-- qrcode-1.5.4.tgz               Local QR code package used by the frontend/tooling.
```

See `src/backend/GamMaSite/README.md`, `src/frontend/README.md`, and `SQL/README.md` for more detail.

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

### Build Frontend

```bash
cd src/frontend
npm install
npm run build
```

The Vite build writes production assets to `src/backend/GamMaSite/wwwroot/react-assets/`.

### Build Backend

```bash
dotnet build src/backend/GamMaSite/GamMaSite.csproj
```

If the local site is already running and locks the debug output, build to a temporary output folder instead:

```bash
dotnet build src/backend/GamMaSite/GamMaSite.csproj -o %TEMP%/GamMaSiteBuildVerify
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
