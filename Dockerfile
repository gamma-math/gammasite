# syntax=docker/dockerfile:1

# ── Stage 1: Build ────────────────────────────────────────────────────────────
ARG BUILD_CONFIGURATION=Release
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION
WORKDIR /src

# Copy project file and restore — separate layer so restore is cached unless csproj changes
COPY GamMaSite.csproj .
RUN dotnet restore GamMaSite.csproj

# Copy remaining sources and publish
COPY . .
RUN dotnet publish GamMaSite.csproj \
        -c "${BUILD_CONFIGURATION}" \
        -o /app/publish \
        --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Use the non-root 'app' user that ships with the base image (UID 1654)
COPY --chown=app:app --from=build /app/publish .

# Pre-create the Data Protection keys directory with correct ownership so the
# volume mount is writable by the app user (Docker creates volumes as root by default)
# TODO: Consider removing this if we switch to a different key storage mechanism that doesn't require a writable directory (e.g. Redis, Azure Key Vault, etc.)
RUN mkdir -p /app/dataprotection-keys && chown app:app /app/dataprotection-keys

USER app

# Serve plain HTTP — TLS is terminated by an upstream reverse proxy (nginx, Traefik, etc.)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "GamMaSite.dll"]
