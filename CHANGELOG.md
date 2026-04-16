# Changelog

All notable changes to this project are documented here.

---

## [Unreleased] — .NET 10 Upgrade — April 2026

### Changed
- **Target framework** upgraded from `net8.0` to `net10.0`
- **.NET SDK** updated to `10.0.202` in `global.json`
- **EF Core** ecosystem upgraded from `8.0.6` to `9.0.0` (pinned — Pomelo MySQL provider has no EF Core 10 release yet)
- **Pomelo.EntityFrameworkCore.MySql** upgraded from `8.0.2` to `9.0.0`
- **Non-EF Microsoft packages** (JwtBearer, OpenIdConnect, Authorization, SpaProxy) upgraded from `8.0.6` to `10.0.6`
- **Ical.Net** upgraded from `4.2.0` to `5.2.1`
- **Stripe.net** upgraded from `45.2.0` to `51.0.0`
- **`Program.cs`**: replaced `app.UseStaticFiles()` with `app.MapStaticAssets()` and chained `.WithStaticAssets()` on route registrations — enables static file fingerprinting and pre-compression (.NET 9+ feature)
- **VS Code `launch.json`**: updated `program` path from `net8.0` to `net10.0`

### Fixed
- **`Services/ICalService.cs`**: replaced `CalDateTime.AsSystemLocal` (removed in Ical.Net 5.x) with `AsUtc.ToLocalTime()` in three extension methods (`ToStartLocalDateTime`, `ToStartWeekday`, `ToWeekOfYear`)

### Resolved conflicts
- `Microsoft.VisualStudio.Web.CodeGeneration.Design` kept at `9.0.0` (not `10.0.2`) because its `10.0.x` release has a hard dependency on EF Core 10, which conflicts with the Pomelo 9.0.0 pin

For full technical details, rationale, and follow-up tasks, see [docs/UPGRADE-NET10.md](docs/UPGRADE-NET10.md).
