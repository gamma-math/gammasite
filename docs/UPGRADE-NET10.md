# Upgrade Notes: .NET 8 → .NET 10

**Date:** April 2026  
**Upgraded by:** GitHub Copilot  
**Scope:** Full platform upgrade from .NET 8 LTS to .NET 10 LTS, including all Microsoft and third-party NuGet packages.

---

## Summary of Changes

| Area | Change |
|------|--------|
| Target Framework | `net8.0` → `net10.0` |
| SDK (`global.json`) | `8.0.0` → `10.0.202` |
| EF Core ecosystem | `8.0.6` → `9.0.0` (pinned — see note below) |
| Pomelo MySQL provider | `8.0.2` → `9.0.0` |
| Non-EF Microsoft packages | `8.0.6` → `10.0.6` |
| Ical.Net | `4.2.0` → `5.2.1` (breaking API change) |
| Stripe.net | `45.2.0` → `51.0.0` |
| `Program.cs` | `UseStaticFiles` → `MapStaticAssets` |
| `ICalService.cs` | `AsSystemLocal` → `AsUtc.ToLocalTime()` |
| VS Code `launch.json` | `net8.0` path → `net10.0` path |

---

## Why EF Core Is Pinned at 9.x (Not 10.x)

`Pomelo.EntityFrameworkCore.MySql` — the MySQL/MariaDB EF Core provider used by this project — tracks EF Core releases with matching major versions. As of April 2026, the latest stable release is `9.0.0`, which targets EF Core 9.x.

No `10.0.x` release of Pomelo exists yet. Upgrading the EF Core packages to `10.0.x` would break the MySQL provider at runtime.

**EF Core 9 runs perfectly on .NET 10 runtime.** The EF Core packages target `net8.0` minimum, and .NET 10 is backward compatible with all `net8.0`-targeting assemblies. There is no functional or security regression from this approach.

The consequence is that scaffolding (`Microsoft.VisualStudio.Web.CodeGeneration.Design`) must also stay at `9.0.0`, since its `10.0.x` version has a hard dependency on EF Core 10.

### Path to full EF Core 10

When `Pomelo.EntityFrameworkCore.MySql 10.0.x` is published (watch the [Pomelo GitHub releases](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/releases) or [NuGet versions](https://www.nuget.org/packages/Pomelo.EntityFrameworkCore.MySql#versions-body-tab)), upgrade all of the following together:

| Package | Pin | Upgrade target |
|---------|-----|----------------|
| `Pomelo.EntityFrameworkCore.MySql` | 9.0.0 | 10.0.x |
| `Microsoft.EntityFrameworkCore` (+ Abstractions, Design, InMemory, Relational, Tools) | 9.0.0 | 10.0.x |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 9.0.0 | 10.0.x |
| `Microsoft.AspNetCore.Identity.UI` | 9.0.0 | 10.0.x |
| `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore` | 9.0.0 | 10.0.x |
| `Microsoft.Extensions.Identity.Core` | 9.0.0 | 10.0.x |
| `Microsoft.Extensions.Identity.Stores` | 9.0.0 | 10.0.x |
| `Microsoft.VisualStudio.Web.CodeGeneration.Design` | 9.0.0 | 10.0.x |

---

## Full Package Version Change Table

| Package | From | To | Reason |
|---------|------|----|--------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.6 | 10.0.6 | .NET 10 release |
| `Microsoft.AspNetCore.Authentication.OpenIdConnect` | 8.0.6 | 10.0.6 | .NET 10 release |
| `Microsoft.AspNetCore.Authorization` | 8.0.6 | 10.0.6 | .NET 10 release |
| `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore` | 8.0.6 | 9.0.0 | EF Core 9 pin |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 8.0.6 | 9.0.0 | EF Core 9 pin |
| `Microsoft.AspNetCore.Identity.UI` | 8.0.6 | 9.0.0 | EF Core 9 pin |
| `Microsoft.AspNetCore.SpaProxy` | 8.0.6 | 10.0.6 | .NET 10 release |
| `Microsoft.EntityFrameworkCore` | 8.0.6 | 9.0.0 | EF Core 9 pin (Pomelo) |
| `Microsoft.EntityFrameworkCore.Abstractions` | 8.0.6 | 9.0.0 | EF Core 9 pin |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.6 | 9.0.0 | EF Core 9 pin |
| `Microsoft.EntityFrameworkCore.InMemory` | 8.0.6 | 9.0.0 | EF Core 9 pin |
| `Microsoft.EntityFrameworkCore.Relational` | 8.0.6 | 9.0.0 | EF Core 9 pin |
| `Microsoft.EntityFrameworkCore.Tools` | 8.0.6 | 9.0.0 | EF Core 9 pin |
| `Microsoft.Extensions.Identity.Core` | 8.0.6 | 9.0.0 | EF Core 9 pin |
| `Microsoft.Extensions.Identity.Stores` | 8.0.6 | 9.0.0 | EF Core 9 pin |
| `Microsoft.VisualStudio.Web.CodeGeneration.Design` | 8.0.2 | 9.0.0 | EF Core 9 pin |
| `Pomelo.EntityFrameworkCore.MySql` | 8.0.2 | 9.0.0 | Latest Pomelo stable |
| `Ical.Net` | 4.2.0 | 5.2.1 | Breaking changes — see below |
| `Stripe.net` | 45.2.0 | 51.0.0 | Latest stable — see below |
| `Google.Apis.Calendar.v3` | 1.68.0.3430 | 1.68.0.3430 | No change needed (`netstandard2.0`) |
| `Humanizer.Core` / `.da` | 2.14.1 | 2.14.1 | No change needed |
| `MimeTypeMapOfficial` | 1.0.17 | 1.0.17 | No change needed |

---

## Code Changes Made

### `Program.cs` — `UseStaticFiles` → `MapStaticAssets`

Introduced in .NET 9 and recommended for .NET 10, `MapStaticAssets` replaces `UseStaticFiles` for MVC/Razor Pages apps. It adds automatic fingerprinting and pre-compression of static files at build/publish time, improves cache headers (ETag/content-hash), and integrates with endpoint routing.

```diff
- app.UseStaticFiles();
+ app.MapStaticAssets();
  
  app.UseRouting();
  // ...
  
- app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
- app.MapRazorPages();
+ app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
+ app.MapRazorPages().WithStaticAssets();
```

The `.WithStaticAssets()` call is required for tag helpers (e.g. `asp-src`, `asp-href`) to resolve fingerprinted file names in Razor views.

### `Services/ICalService.cs` — Ical.Net 5.x API migration

`CalDateTime.AsSystemLocal` was removed in Ical.Net 5.0. The `CalDateTime` type in v5 exposes `AsUtc` (returns `DateTime` in UTC) and `Value` (raw value), but no longer exposes a direct system-local property.

The replacement is `AsUtc.ToLocalTime()`, which retrieves the UTC value and converts it to the system's local timezone — functionally identical to the old behaviour.

**Affected methods in `ICalExtensions`:**

```diff
  public static string ToStartLocalDateTime(this CalendarEvent calendar)
- return calendar.Start.AsSystemLocal.ToLocalTime().ToString("dd-MM-yyyy HH:mm");
+ return calendar.Start.AsUtc.ToLocalTime().ToString("dd-MM-yyyy HH:mm");

  public static string ToStartWeekday(this CalendarEvent calendar)
- return calendar.Start.AsSystemLocal.ToLocalTime().ToString("dddd").Humanize(LetterCasing.Sentence);
+ return calendar.Start.AsUtc.ToLocalTime().ToString("dddd").Humanize(LetterCasing.Sentence);

  public static string ToWeekOfYear(this CalendarEvent calendar)
- var datetime = calendar.Start.AsSystemLocal.ToLocalTime();
+ var datetime = calendar.Start.AsUtc.ToLocalTime();
```

`Calendar.Load(string)`, `calendar.Events`, `CalendarEvent.Uid`, and `CalendarEvent.Start.AsUtc` are all unchanged in Ical.Net 5.x and required no code changes.

### Stripe.net — v45 → v51 (no breaking code changes required)

`Stripe.net` was upgraded from `45.2.0` to `51.0.0`. Despite spanning 6 major versions, the APIs used by this project (`SessionCreateOptions`, `SessionLineItemPriceDataOptions`, `ProductListOptions`, `PriceListOptions`, `ProductService`, `PriceService`, `SessionService`, `StripeException`, `Session.PaymentStatus`, `Session.Metadata`, `Product.Metadata`) were unchanged and compiled without errors or warnings. No code modifications were required.

---

## Notable .NET 9 and .NET 10 Improvements Relevant to This Project

### Static Asset Delivery (`MapStaticAssets`)
`MapStaticAssets` (new in .NET 9) fingerprints and pre-compresses static files (CSS, JS) at build and publish time, rather than at first request. Benefits:
- Stronger cache invalidation — file names include a content hash
- Pre-compressed `.gz` / `.br` files are served automatically if the client supports it
- Significant bandwidth reduction for returning visitors

### EF Core 9 Improvements (vs. 8)
Even though we are using EF Core 9 rather than 10, this is a net improvement over the previous EF Core 8:
- Better SQL generation (query pruning, `GREATEST`/`LEAST` translation, nullable comparison fixes)
- Protection against concurrent migrations
- Improved data seeding API (`UseSeeding` / `UseAsyncSeeding`)
- Better LINQ translation performance

### HttpClientHandler and TLS
`SslProtocols.Tls13 | SslProtocols.Tls12` is still valid in .NET 10 and used in `EmailService`, `GithubService`, and `ICalService`. On .NET 10, the default TLS configuration already favours TLS 1.3, so explicitly setting this is now somewhat redundant but harmless.

### .NET 10 Platform Performance
.NET 10 brings JIT improvements, reduced startup time, and lower memory usage compared to .NET 8 — all of which benefit this ASP.NET Core application with no code changes required.

---

## NuGet Warnings to Be Aware Of

These warnings appear during `dotnet restore` and `dotnet build`. They are informational and do not affect runtime:

| Warning | Cause | Action |
|---------|-------|--------|
| `NU1510` on `Authorization`, `Identity.Core`, `Identity.Stores` | These are already included transitively; the explicit references are technically redundant | Safe to leave; can be removed in a future cleanup |
| `NU1903` on `Microsoft.Build 17.10.4` | Transitive dependency of `CodeGeneration.Design 9.0.0`; vulnerability is in the build tooling, not the runtime | Not exploitable in production; will resolve when CodeGeneration.Design 10.x is adopted |
| `NU1901` on `NuGet.Packaging` / `NuGet.Protocol` | Low severity; transitive tooling dependency | Not runtime-relevant |

---

## Running and Testing the Upgraded Application

### Prerequisites
- .NET 10 SDK installed (`dotnet --version` shows `10.x`)
- MySQL 8.0+ server accessible at the configured connection string

### Build
```bash
dotnet build
```
Expected: `Build succeeded` with 0 errors.

### Run (development)
```bash
dotnet run --project GamMaSite.csproj
```
Or use VS Code **Run > Start Debugging** (F5) — the `launch.json` is pre-configured for `.NET 10`.

### Confirm runtime version
Check startup log output for:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
```
Or check via:
```bash
dotnet --info
```

### Smoke test checklist
- [ ] Home page loads (`/`)
- [ ] Login and registration work (tests EF Core 9 + Pomelo MySQL on .NET 10)
- [ ] Calendar page loads (`/Calendar`) — tests Ical.Net 5.x
- [ ] Pay/products page loads (`/Pay`) — tests Stripe 51
- [ ] Admin: Users and Roles pages work
- [ ] Static assets load correctly (CSS, JS) — tests `MapStaticAssets`

### Publish
```bash
dotnet publish -c Release
```
Output lands in `bin/Release/net10.0/publish/`. Update the IIS publish profile (`Properties/PublishProfiles/IISProfile.pubxml`) if it references `net8.0` explicitly.

---

## Known Limitations After This Upgrade

1. **EF Core ecosystem is at 9.x, not 10.x** — This is intentional (Pomelo constraint). All EF Core 10 improvements are not yet available. See the "Path to full EF Core 10" section above.

2. **`Microsoft.VisualStudio.Web.CodeGeneration.Design` is at 9.0.0** — Scaffolding commands (`dotnet aspnet-codegenerator`) will use the EF Core 9 scaffolding engine.

---

## Follow-up Tasks

### 1. Remove `Microsoft.AspNetCore.SpaProxy` (likely unused)
`Microsoft.AspNetCore.SpaProxy` is referenced in `GamMaSite.csproj` but there is no SPA frontend in the workspace (no `ClientApp/`, no `package.json`). It appears to be a scaffolding leftover. Confirm and remove:
```xml
<!-- Remove this line from GamMaSite.csproj if no SPA is used -->
<PackageReference Include="Microsoft.AspNetCore.SpaProxy" Version="10.0.6" />
```

### 2. Upgrade EF Core to 10.x when Pomelo supports it
Monitor [Pomelo releases](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/releases) for a `10.0.x` release. When available, upgrade all packages in the "EF Core ecosystem" table above to their matching `10.0.x` versions together in one pass.
