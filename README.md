# gammasite
.NET-based version of the Gamma website, built with the following technologies.

* ASP.NET Core (.NET 10)
* MySQL 8.0+ (via Pomelo EF Core provider)
* ASP.NET Core Identity (authentication)
* Stripe (payments)
* Ical.Net (calendar)

## Branching workflow

* `master` is the production and default branch.
* `staging` is the test and integration branch before changes are promoted to `master`.
* Create new features in `feature/*` branches and merge them into `staging` via Pull Requests.
* Create bug fixes in `fix/*` branches and merge them into `staging` via Pull Requests.
* Promote tested changes from `staging` to `master` via Pull Requests.
* If needed, urgent fixes can be created in `fix/*` and merged directly into `master` via Pull Requests.
* Direct pushes to `master` and `staging` are not allowed.

## Getting started

### Prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/download) (`dotnet --version` should show `10.x`)
* [Node.js 22+](https://nodejs.org/) (`node --version` should show `22.x`)
* MySQL 8.0+ server running and accessible via the connection string in `appsettings.json`

### Build and run

The project includes a `Makefile` with the most commonly used commands:

| Command                     | Description                                          |
|-----------------------------|------------------------------------------------------|
| `make build`                | Build the solution (Debug)                           |
| `make run`                  | Start the application (Development)                  |
| `make run ENV=Staging`      | Start the application with the Staging environment   |
| `make run ENV=Production`   | Start the application with the Production environment|
| `make watch`                | Start with hot reload (Development)                  |
| `make watch ENV=Staging`    | Start with hot reload in the Staging environment     |
| `make dev`                  | Start backend + Vite dev server together (see below) |
| `make dev-spa`              | Start the Vite dev server only                       |
| `make build-spa`            | Build the React SPA into `wwwroot/spa/`              |
| `make publish`              | Publish a Release build to `bin/Release/`            |
| `make restore`              | Restore NuGet packages                               |
| `make clean`                | Delete `bin/` and `obj/`                             |

```bash
make run                  # Development (default)
make run ENV=Staging
make run ENV=Production
```

The application starts on **https://localhost:5001** (HTTP: http://localhost:5000). The environment is controlled via the `ENV` variable (default: `Development`).

Alternatively, using `dotnet` directly:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project GamMaSite.csproj
```

### VS Code

Open the project in VS Code and press **F5** — the debug configuration in `.vscode/launch.json` will build and start automatically with the Development environment.

---

## Frontend development (React SPA)

The React SPA lives in `ClientApp/` and is built with Vite. In production the compiled output sits in `wwwroot/spa/` and is served as static files by .NET. In development you run both processes in parallel so you get Vite's hot module replacement (HMR).

### Quickstart

```bash
cd ClientApp && npm install   # first time only
cd ..                         # back to repo root
make dev                      # starts backend + Vite in parallel
```

Then open **http://localhost:5173** (the Vite URL) in your browser — **not** the .NET port.

### How it works

`make dev` runs `make watch` (the .NET backend with hot reload on `https://localhost:5001`) and `make dev-spa` (the Vite dev server on `http://localhost:5173`) in parallel.

Vite is already configured to proxy `/api` and `/Identity` requests to the .NET backend, so authentication and API calls work transparently from the Vite port. You never need to deal with CORS in development.

### If you only need to check the built SPA

```bash
make build-spa   # compiles React into wwwroot/spa/
make watch       # serve via .NET at https://localhost:5001
```

Note that this skips HMR — you must re-run `make build-spa` after each frontend change.

---

## References

* https://github.com/jasonsturges/mysql-dotnet-core
* https://retifrav.github.io/blog/2018/03/20/csharp-dotnet-core-identity-mysql/
* https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity
* https://docs.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model
* https://docs.microsoft.com/en-us/aspnet/core/security/authentication/scaffold-identity
* https://docs.microsoft.com/en-us/aspnet/core/security/authentication/add-user-data
* https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration
* https://www.yogihosting.com/aspnet-core-identity-roles/
