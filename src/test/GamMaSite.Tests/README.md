# GamMaSite.Tests

This project contains automated backend tests for the GamMaSite ASP.NET Core application.
The tests cover React API controllers, application services, Identity page models, validation rules, privacy behavior, and payment state handling.

## Requirements

- .NET SDK version specified by `global.json`
- A working checkout of the repository

The tests use mocks and an in-memory Entity Framework database where possible. They do not require the development database, SMTP server, Stripe account, or external calendar services.

## Run All Tests

From the repository root:

```powershell
dotnet test GamMaSite.sln
```

To run the test project directly:

```powershell
dotnet test src/test/GamMaSite.Tests/GamMaSite.Tests.csproj
```

For a faster run when dependencies are already restored:

```powershell
dotnet test GamMaSite.sln --no-restore
```

## Run Selected Tests

Run one test class:

```powershell
dotnet test GamMaSite.sln --filter FullyQualifiedName~ApiAccountControllerTests
```

Run tests matching a method name:

```powershell
dotnet test GamMaSite.sln --filter FullyQualifiedName~Login_SuccessFallsBackToReactForExternalReturnUrl
```

Run tests from a specific namespace:

```powershell
dotnet test GamMaSite.sln --filter FullyQualifiedName~GamMaSite.Tests
```

## Test Organization

- `Api*ControllerTests.cs` covers React API endpoints and HTTP response behavior.
- `*ServiceTests.cs` covers application service rules using mocks or an in-memory database.
- `RegisterModelTests.cs` and `ManageIndexModelTests.cs` cover ASP.NET Identity page models.
- `TestDoubles.cs` contains shared Identity mocks, claims, and page context helpers.

## Notes

External integrations are not called by the unit tests. Stripe, SMTP, Google Calendar, and other external services are mocked or tested through local validation and mapping rules.

The test project is included in `GamMaSite.sln`, so running the solution-level command also builds the application and executes this test project.
