# GamMaSite Backend

This folder contains the ASP.NET Core application that hosts the site, serves the React shell, exposes API endpoints, and owns Identity, database access, email, payments, and integrations.

## Structure

Only tracked project folders/files are listed here:

```text
GamMaSite/
|-- Areas/
|   `-- Identity/                  ASP.NET Core Identity Razor pages and account/security flows.
|-- Controllers/                   React shell routing, API controllers, Stripe endpoints, and legacy MVC controllers.
|-- Data/                          EF Core database context and Identity/data conversion configuration.
|-- Models/                        Identity extensions, content entities, registrations, templates, enums, and legacy view models.
|-- Properties/                    Launch, publish, and service dependency configuration.
|-- Services/                      Business and integration services for content, email, SMS, Stripe, calendar, and library data.
|-- ViewModels/                    API DTOs and legacy payment view models.
|-- Views/
|   |-- React/                     Razor shell that loads the built React app.
|   |-- Shared/                    Shared Razor layout/error partials.
|   |-- Home/                      Legacy MVC views.
|   |-- Users/                     Legacy MVC views.
|   |-- Role/                      Legacy MVC views.
|   |-- Messages/                  Legacy MVC views.
|   |-- Calendar/                  Legacy MVC views.
|   |-- Library/                   Legacy MVC views.
|   `-- Pay/                       Legacy/payment Razor views.
|-- wwwroot/                       Static assets, shared CSS/JS, third-party libraries, logo, and built React assets.
|-- Program.cs                     Service registration, middleware, redirects, routing, and startup.
`-- GamMaSite.csproj               Backend project and NuGet dependencies.
```

## Active Runtime Responsibilities

* Host the React app and map `/react/*` routes to the React shell.
* Redirect selected old MVC/Identity URLs to React routes.
* Provide authenticated JSON APIs for React.
* Manage users, roles, account state, and security through ASP.NET Core Identity.
* Persist content, links, registrations, templates, and user data through EF Core/MySQL.
* Send email/SMS messages and render system/admin templates.
* Create Stripe checkout sessions and handle payment-related redirects.

## Legacy Policy

Legacy MVC controllers and views are not deleted yet. They are marked with `LEGACY MVC` comments and should be reviewed before removal.

Be especially careful with:

* Payment success/cancel routes, because Stripe or old links may still reference them.
* Identity Razor pages, because several security-sensitive flows are intentionally still server-rendered.
* Shared Razor layout files, because Identity pages still use them.

## Build

```bash
dotnet build src/backend/GamMaSite/GamMaSite.csproj
```

If the local app is running and locks the default build output, use a temporary output folder:

```bash
dotnet build src/backend/GamMaSite/GamMaSite.csproj -o %TEMP%/GamMaSiteBuildVerify
```
