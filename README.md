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

## References

* https://github.com/jasonsturges/mysql-dotnet-core
* https://retifrav.github.io/blog/2018/03/20/csharp-dotnet-core-identity-mysql/
* https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity
* https://docs.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model
* https://docs.microsoft.com/en-us/aspnet/core/security/authentication/scaffold-identity
* https://docs.microsoft.com/en-us/aspnet/core/security/authentication/add-user-data
* https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration
* https://www.yogihosting.com/aspnet-core-identity-roles/
