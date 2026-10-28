# Lokal Docker-udvikling

Dette setup starter GamMaSite med de to databaser, som applikationen bruger:

- MySQL 8 til Identity, content, events, betalinger og øvrige GamMaSite-data.
- PostgreSQL 16 til Finance-databasen.
- GamMaSite-backenden, som bygger og serverer den eksisterende React/Vite-frontend.

Alle nye filer til dette setup ligger i denne mappe. Compose bruger repository-roden som build context, så frontend- og backendkoden bliver i deres eksisterende mapper.

Runner-kommandoerne ligger samlet i [scripts](./scripts/). Docker-specifikke PostgreSQL-initfiler ligger separat i [postgres-init](./postgres-init/). Den fælles hjælpeimage bygges som `local-tools`-target i [Dockerfile](./Dockerfile), så lokaludviklingen kun har én Dockerfile.

## Forudsætninger

- Docker Engine eller Docker Desktop med Compose v2.
- Vipps-credentials, hvis Vipps-webhooks skal registreres eller roteres.

Alle hjælpe- og integrationsscripts kan køres inde i Docker. På Windows er det derfor ikke nødvendigt at åbne Git for Windows. Linux kan bruge de tilsvarende `docker compose --profile tools run ...`-kommandoer direkte.

Scripts kan køres direkte gennem Compose. Fra denne mappe er den korte form:

```bash
docker compose --profile tools run --rm --build tools scripts/validate.sh
```

Seed køres som en dedikeret engangsservice:

```bash
docker compose --profile tools run --rm --build seed
```

`tools`-servicen monterer repositoryet og Docker-socketen, så scripts kan styre de øvrige Compose-services. Det er en lokal udviklingsfunktion og bør kun køres med en betroet checkout.

## Første opsætning

Kør fra repository-roden:

```bash
cd src/services/local-development
```

Der er ikke krav om at kopiere `.env.example` til `.env`. Compose-filen har lokale standardvaerdier for database- og portkonfigurationen, saa standardstacken kan bygges og startes direkte. `.env` er fortsat en valgfri, git-ignoreret override-fil til lokale credentials og integrationer. Brug kun lokale testcredentials; indsaet ikke produktionssecrets.

Kontrollér konfigurationen uden at starte containere:

```bash
docker compose config --quiet
```

`.env.example` er dokumentation for alle understøttede variabelnavne. Stripe- og Vipps-secrets skal ikke udfyldes her; de genereres af integrationsscripts og gemmes kun i den lokale `.env`.

## Step-by-step guide

Alle kommandoer nedenfor køres fra `src/services/local-development/`.

### 1. Første gang

Start databaserne, byg backend/frontend-image og start hjemmesiden:

```bash
docker compose up -d --build
```

Kontrollér derefter status og forbindelser:

```bash
docker compose ps
docker compose --profile tools run --rm --build tools scripts/validate.sh
```

Hvis der skal bruges testdata, og databaserne er helt tomme, køres seed eksplicit:

```bash
docker compose --profile tools run --rm --build seed
```

Seed-servicen kører inde i Docker med databaseklienterne til MySQL og PostgreSQL. Den afviser ikke-tomme databaser og registrerer, hvis seed allerede er kørt. Den er derfor ikke en del af den almindelige opstart, og den kræver ikke Git for Windows eller lokale databaseklienter.

Før seed harmoniserer servicen den lokale `AspNetRoleClaims`-tabel til `utf8mb4_general_ci`, så den kan sammenlignes med de øvrige lokale rettighedstabeller på MySQL 8.

Hvis hjælpe-imaget allerede er bygget, kan man også køre seed-servicen direkte:

```bash
docker compose --profile tools run --rm seed
```

Seed køres direkte som Compose-engangsservice med kommandoen ovenfor.

### 2. Start Stripe CLI og webhook-tunnel uden åbne terminaler

Disse workflows ændrer ikke databasevolumes og kører ikke migrations. Start begge integrationsservices detached med én kommando:

```bash
docker compose --profile tools run --rm --build tools scripts/integrations-up.sh
```

Kommandoen starter Stripe CLI og Cloudflare Tunnel som permanente Compose-services, opsamler `STRIPE_WEBHOOK_SECRET` og `PUBLIC_WEBHOOK_BASE_URL`, opdaterer den lokale `.env` og genstarter kun `app`-containeren. Kommandoen afslutter derefter, mens integrationsservices fortsætter i baggrunden.

Hvis `STRIPE_SECRET_API_KEY` er en restricted Stripe test-key (`rk_test_...`), skal key’en have permissionen `Debugging Tools` → `Write` (`stripecli_session_write`). Uden denne permission kan Stripe CLI ikke oprette en listen-session. Brug kun en test-mode key til lokal udvikling.

Den samme key bruges også af GamMaSite-backenden. Giv derfor følgende mindst nødvendige permissions:

| Stripe resource | Permission | Bruges til |
|---|---|---|
| Debugging Tools | Write | Stripe CLI `listen` og generering af webhook-sessionen |
| Checkout Sessions | Read + Write | Oprette og hente Checkout Sessions |
| Products | Read + Write | Læse, oprette og aktivere/deaktivere produkter |
| Prices | Read + Write | Læse, oprette og opdatere priser |
| Subscriptions | Read + Write | Læse, ændre og annullere abonnementer |
| Invoices | Read | Vise betalingshistorik for abonnementer |

`STRIPE_PUBLIC_API_KEY` er en separat publishable test-key og skal ikke have disse server-permissions. `STRIPE_WEBHOOK_SECRET` er et webhook-signing secret og er ikke en API-key.

#### Vipps/MobilePay

Vis først de aktive Vipps-webhooks og deres ID’er:

```bash
docker compose --profile tools run --rm --build tools scripts/vipps-webhook.sh list
```

Kopiér ID’et før kolon (`:`) på den webhook, der skal udskiftes, og brug det som `OLD_WEBHOOK_ID`:

Når kommandoen køres via `local-development`, bruges Vipps-credentials og `VIPPS_MSN` fra denne lokale `.env`. Den separate [Vipps Compose-opsætning](../vipps/docker-compose.yml) har fortsat sin egen `.env`, men den overskrives ikke af denne lokale wrapper.

```bash
docker compose --profile tools run --rm --build tools scripts/vipps-webhook.sh register
```

Ved rotation:

```bash
docker compose --profile tools run --rm --build tools scripts/vipps-webhook.sh rotate OLD_WEBHOOK_ID
```

Vipps-scriptet gemmer den nye `VIPPS_WEBHOOK_SECRET` og genstarter kun `app`. Tunnelen kører detached som en Compose-service.

Kontrollér eller stop integrationsservices:

```bash
docker compose --profile integrations ps
docker compose --profile integrations logs -f stripe-cli tunnel
docker compose --profile integrations stop stripe-cli tunnel
```

### 3. Genbyg kun frontend og backend

Hvis databaserne allerede kører, og der kun er ændret i applikationskode eller frontend, bruges:

```bash
docker compose up -d --build --no-deps app
```

Det bygger Docker-imaget igen og genopretter kun `app`. MySQL- og PostgreSQL-containere eller volumes opdateres ikke.

Ved behov for et helt nyt image uden Docker-cache:

```bash
docker compose build --no-cache app
docker compose up -d --no-deps app
```

Kontrollér efterfølgende:

```bash
docker compose --profile tools run --rm --build tools scripts/validate.sh
docker compose logs -f app
```

### 4. Andre almindelige situationer

Start eksisterende containere igen uden rebuild:

```bash
docker compose up -d
```

Genstart kun hjemmesiden uden rebuild:

```bash
docker compose restart app
```

Se status og logs:

```bash
docker compose ps
docker compose logs -f app
docker compose logs -f mysql finance-postgres
```

Kør en engangskommando i app-imaget uden at beholde containeren:

```bash
docker compose run --rm --no-deps app <kommando>
```

Hvis database-schema eller seed-filer ændres, er en app-rebuild ikke nok. Init-SQL-filerne køres kun automatisk, når et database-volume oprettes første gang. Der er ikke fundet EF Core migrationsfiler i projektet, så databaseændringer skal fortsat håndteres som en særskilt, gennemgået SQL-ændring. Brug ikke `down -v` for at tvinge en ændring igennem.

Stop stacken uden at slette database- eller medievolumes:

```bash
docker compose down
```

Der findes med vilje ingen automatisk reset-kommando. Brug ikke `docker compose down -v`, medmindre alle lokale data med vilje skal slettes.

## Lokale adresser

- GamMaSite: `http://localhost:5000`
- MySQL: `localhost:3306`
- Finance PostgreSQL: `localhost:5434`

Portene kan ændres i `.env`. Databaseportene bindes kun til loopback-interfacet.

## Databaseinitialisering

MySQL initialiseres første gang det nye named volume er tomt med [SQL/AspNetMysql.test.sql](../../../SQL/AspNetMysql.test.sql). Finance PostgreSQL initialiseres første gang med [SQL/finance/PostgreSQLTest.sql](../../../SQL/finance/PostgreSQLTest.sql), lokale database-roller og de eksisterende Finance-rettigheder.

De eksisterende databaseprojekter under `Documents/DB` blev inspiceret, men deres Windows-specifikke datafoldere bindes ikke ind i dette setup. Det gør løsningen Linux-kompatibel og sikrer, at eksisterende databasevolumes ikke overskrives. Compose opretter isolerede volumes med navnene:

- `gammasite-local-development-mysql-data`
- `gammasite-local-development-finance-data`
- `gammasite-local-development-content-media`

Der blev ikke fundet EF Core migrationsfiler i repositoryets backend. Derfor bruger dette setup de eksisterende SQL-schemafiler ved første initialisering; det opfinder ikke migrationsfiler eller kører dem automatisk.

Testdata køres kun ved eksplicit brug af `--seed` eller den separate Compose-seed-service og kommer fra:

- `SQL/test-seed/00_seed_all.sql`
- `SQL/finance/test-seed/00_gamma_finance_seed.sql`

Seed-servicen bruger det fælles `local-tools`-target i [Dockerfile](./Dockerfile) og læser seed-filerne fra repository-mountet. Den opretter ikke en permanent container.

## Validering

Når stacken kører, kontrollerer følgende kommando Compose-konfiguration, container-health, MySQL, PostgreSQL og den lokale HTTP-adresse:

```bash
docker compose --profile tools run --rm --build tools scripts/validate.sh
```

Valideringen viser kun manglende variabelnavne og statusbeskeder. Den udskriver ikke secret-værdier, passwords eller connection strings.

## Stripe-webhooks

Stripe CLI kører som en valgfri, permanent Docker Compose-service. Signing secretet er dynamisk for den aktive `stripe listen`-session; integrations-scriptet opsamler det uden at vise det, skriver det til den lokale `.env` og genstarter kun `app`-servicen.

1. Opret en test-mode restricted Stripe API key og læg den i `STRIPE_SECRET_API_KEY` i den lokale `.env`.
2. Sørg for at GamMaSite kører med `docker compose up -d`.
3. Start Stripe CLI og tunnelen samlet i Docker:

```bash
docker compose --profile tools run --rm --build tools scripts/integrations-up.sh
```

Scriptet forwarder de eventtyper, som applikationen allerede behandler, til:

```text
/api/payments/stripe/webhook
```

Der oprettes ikke eksterne webhook endpoints af dette setup. Secretet gælder kun, så længe den aktuelle Stripe CLI-service kører; kør integrations-scriptet igen, hvis servicen eller sessionen oprettes på ny.

## Webhook-tunnel og MobilePay

Cloudflare Quick Tunnel startes sammen med Stripe CLI:

```bash
docker compose --profile tools run --rm --build tools scripts/integrations-up.sh
```

Scriptet venter på Cloudflare Quick Tunnel-URL'en, gemmer den som `PUBLIC_WEBHOOK_BASE_URL` i den lokale `.env` og viser de to relevante endpoints:

```text
/api/payments/stripe/webhook
/api/payments/vipps/webhook
```

URL'en er midlertidig og må ikke bruges som permanent TEST- eller PROD-webhook. Tunnel-containeren skal blot fortsætte som detached Compose-service.

### Vipps webhook-secret

Når integrationsprofilen kører, kan en ny Vipps-webhook registreres og secretet gemmes automatisk i den lokale `.env`:

```bash
docker compose --profile tools run --rm --build tools scripts/vipps-webhook.sh register
```

Det svarer til URL'en `${PUBLIC_WEBHOOK_BASE_URL}/api/payments/vipps/webhook`. En anden URL kan gives eksplicit:

```bash
docker compose --profile tools run --rm --build tools scripts/vipps-webhook.sh register --url https://example.test/api/payments/vipps/webhook
```

Ved rotation bruges det eksisterende webhook-ID. Den gamle webhook bliver med vilje ikke slettet automatisk:

```bash
docker compose --profile tools run --rm --build tools scripts/vipps-webhook.sh rotate OLD_WEBHOOK_ID
```

Wrapperen genbruger Vipps-værktøjet og [Vipps README](../vipps/README.md) for credentials. Den sender ikke secretet til terminalen; ved fejl ændres den lokale `VIPPS_WEBHOOK_SECRET` ikke. Efter en vellykket registrering eller rotation genstartes `app`, så den nye secret anvendes.

## Mailpit-begrænsning

Mailpit er ikke startet i dette setup, fordi den nuværende [EmailService](../../backend/GamMaSite/Services/EmailService.cs) sender direkte til Mailguns HTTP-API. Den kan derfor ikke bruge Mailpit som SMTP-server via eksisterende miljøvariabler alene.

For at understøtte Mailpit kræves en separat applikationsændring, eksempelvis en SMTP-baseret sender eller en konfigurerbar mail-provider. Det ligger uden for denne runner-opgave og er ikke udført.

## Valgfri integrationsprofil

Standardopstarten starter kun MySQL, Finance PostgreSQL og GamMaSite. Hjælpeimage, Stripe CLI, Vipps-webhookregistrering og tunnel startes separat, så eksterne integrationer ikke er nødvendige for almindelig lokal udvikling.

Compose indeholder desuden en valgfri `integrations`-profil for Stripe CLI og en Cloudflare Quick Tunnel:

```bash
docker compose --profile integrations up -d stripe-cli tunnel
docker compose --profile integrations ps
docker compose --profile integrations stop stripe-cli tunnel
```

Denne profil er valgfri og kræver ingen ændring af kontoindstillinger. En administreret Cloudflare Tunnel med token eller domæne skal konfigureres manuelt af brugeren og er ikke en del af standardopsætningen.

## Almindelige fejl

- `unable to get image ... dockerDesktopLinuxEngine ... The system cannot find the file specified`: Docker CLI'en er installeret, men Docker Desktop's Linux-engine kører ikke. Start Docker Desktop, vent til status viser, at engine er startet, og kontrollér derefter med `docker info`, at både Client og Server vises uden fejl. Sørg også for, at Docker Desktop bruger Linux-containere. Kør derefter `docker compose up -d --build` igen.
- `Missing .env.example`: kontrollér at du står i `src/services/local-development/` og at runner-filerne er komplette. `.env` er valgfri ved standardopstart.
- `ports are not available` for `5433` eller `3306`: porten bruges allerede af en lokal PostgreSQL-/MySQL-installation eller en anden service. Sæt eksempelvis `FINANCE_POSTGRES_PORT=5434` eller `MYSQL_PORT=3307` i den lokale `.env`, og start derefter stacken igen. Appens interne forbindelser bruger fortsat Compose-servicenavnene og containerportene `5432`/`3306`.
- En database er ikke healthy: kør `docker compose logs mysql` eller `docker compose logs finance-postgres`.
- GamMaSite starter ikke: kontrollér `docker compose logs app`; appen venter på begge database-healthchecks.
- `ERROR 1267 ... Illegal mix of collations` under seed: kør seed-kommandoen igen. Seed-servicen harmoniserer den lokale claims-tabel automatisk; ingen databasevolumes skal slettes.
- HTTP-check fejler: kontrollér `APP_PORT` og `docker compose logs app`.
- Stripe webhook returnerer fejl: kontrollér `docker compose --profile integrations logs stripe-cli`, API-key-permissions og at `STRIPE_WEBHOOK_SECRET` svarer til den aktive CLI-session.
- MobilePay-webhooken returnerer unauthorized: kontrollér den lokale `VIPPS_WEBHOOK_SECRET`, den registrerede URL og `docker compose --profile integrations ps tunnel`.
