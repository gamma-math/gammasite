# ASPNETCORE_ENVIRONMENT controls the runtime environment.
# Valid values: Development | Staging | Production
# Override at the command line: make run ENV=Staging
ENV ?= Development

.PHONY: build run watch publish restore clean

## Build the solution (Debug)
build:
	dotnet build GamMaSite.sln

## Restore NuGet packages
restore:
	dotnet restore GamMaSite.sln

## Run the application (default: Development)
run: export ASPNETCORE_ENVIRONMENT = $(ENV)
run:
	dotnet run --project GamMaSite.csproj

## Run with hot reload (watches for file changes, default: Development)
watch: export ASPNETCORE_ENVIRONMENT = $(ENV)
watch:
	dotnet watch run --project GamMaSite.csproj

## Build the React SPA (outputs to wwwroot/spa/)
build-spa:
	npm --prefix ClientApp run build

## Publish a Release build (builds SPA first, then .NET)
publish: build-spa
	dotnet publish GamMaSite.sln -c Release

## Remove build and publish output
clean:
	dotnet clean GamMaSite.sln
	rm -rf bin/ obj/
