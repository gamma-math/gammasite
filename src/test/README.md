# Tests

`src/test/` contains the repository's backend, frontend and database-tooling tests. The standalone Stripe and MobilePay/Vipps helper tests live under `src/services/*/test/` and are documented separately in the published codebase documentation.

Run the suites from the repository root:

```powershell
dotnet test src/test/GamMaSite.Tests/GamMaSite.Tests.csproj
Push-Location src/frontend
npm test
Pop-Location
python -m unittest discover -s src/test/database -p "test_*.py"
```

The database tests validate schema parsing, comparison and deployment safety. They do not require a live database; integration testing against MySQL or PostgreSQL is documented separately in the database service documentation.

Run the standalone service tests with their own virtual environments:

```powershell
& .\src\services\stripe\.venv\Scripts\python.exe -m pytest src/services/stripe/test
& .\src\services\vipps\.venv\Scripts\python.exe -m pytest src/services/vipps/test
```

Those tests mock external HTTP calls and do not require Stripe or MobilePay/Vipps credentials.
