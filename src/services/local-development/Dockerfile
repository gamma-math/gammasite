FROM docker@sha256:851f91d241214e7c6db86513b270d58776379aacc5eb9c4a87e5b47115e3065c AS local-tools

RUN apk add --no-cache \
    bash \
    coreutils \
    curl \
    gawk \
    grep \
    mariadb-client \
    postgresql16-client \
    sed

ENTRYPOINT ["bash"]

FROM node@sha256:4b150b48b4116e2a22f2c39cc27025fbf4f153e2078fd3b14d159fdb5c7279da AS frontend-build

WORKDIR /workspace

COPY src/frontend/package.json src/frontend/package-lock.json src/frontend/
COPY src/frontend/vendor src/frontend/vendor
RUN npm ci --prefix src/frontend

COPY src/frontend src/frontend
RUN mkdir -p src/backend/GamMaSite/wwwroot
RUN npm --prefix src/frontend run build

FROM mcr.microsoft.com/dotnet/sdk@sha256:de4fe90a98845d75910b9769662465c84de1a934665c431279d1382b2bddc02b AS backend-build

WORKDIR /src

COPY src/backend/GamMaSite/GamMaSite.csproj src/backend/GamMaSite/
RUN dotnet restore src/backend/GamMaSite/GamMaSite.csproj

COPY src/backend/GamMaSite src/backend/GamMaSite
COPY --from=frontend-build /workspace/src/backend/GamMaSite/wwwroot/react-assets src/backend/GamMaSite/wwwroot/react-assets
RUN dotnet restore src/backend/GamMaSite/GamMaSite.csproj
RUN dotnet publish src/backend/GamMaSite/GamMaSite.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet@sha256:de0ee358cdd672f7017a003bed21e9ac9ed48a27c922a59b18175b6e82fa7fee

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=backend-build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=5s --start-period=20s --retries=12 \
    CMD curl --fail --silent http://127.0.0.1:8080/ > /dev/null || exit 1

ENTRYPOINT ["dotnet", "GamMaSite.dll"]
