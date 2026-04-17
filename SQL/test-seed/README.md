# Test Seed

Denne mappe indeholder fake testdata til GAMMASITE, saaledes at databasen hurtigt kan seedes lokalt til udvikling og test.

Seed-filerne bruger kun testdata og indeholder ikke rigtige persondata eller produktionshemmeligheder.

## Kendte loginoplysninger

- Admin: `admin.test@gamma.local` / `Admin123!Test`
- Bruger: `bruger.test@gamma.local` / `Bruger123!Test`
- Oevrige testbrugere: `Bruger123!Test`

## Hvad der faktisk seedes

Foelgende scripts indsaetter testdata:

1. `01_AspNetRoles.sql`
2. `02_AspNetUsers.sql`
3. `03_AspNetUserRoles.sql`
4. `07_AspNetUserTokens.sql`

Foelgende scripts rydder kun tabellerne og efterlader dem tomme:

1. `04_AspNetRoleClaims.sql`
2. `05_AspNetUserClaims.sql`
3. `06_AspNetUserLogins.sql`
4. `08_DeviceCodes.sql`
5. `09_Keys.sql`
6. `10_PersistedGrants.sql`

Foelgende scripts indsaetter ikke data i deres nuvaerende form:

1. `11_UserOverview.sql`
2. `12___EFMigrationsHistory.sql`

## Koerselsrekkefolge

Hvis du vil koere scripts enkeltvist, brug denne rekkefolge:

1. `01_AspNetRoles.sql`
2. `02_AspNetUsers.sql`
3. `03_AspNetUserRoles.sql`
4. `04_AspNetRoleClaims.sql`
5. `05_AspNetUserClaims.sql`
6. `06_AspNetUserLogins.sql`
7. `07_AspNetUserTokens.sql`
8. `08_DeviceCodes.sql`
9. `09_Keys.sql`
10. `10_PersistedGrants.sql`
11. `11_UserOverview.sql`
12. `12___EFMigrationsHistory.sql`

Hvis du vil have de vigtigste testdata ind hurtigt, kan du koere `00_seed_all.sql`.

`00_seed_all.sql` rydder og seeder kun disse tabeller:

1. `AspNetRoles`
2. `AspNetUsers`
3. `AspNetUserRoles`
4. `AspNetUserTokens`

## Bemaerkninger

- `Keys` holdes bevidst tom, saa testmiljoeet kan generere sine egne signeringsnoegler.
- `DeviceCodes` og `PersistedGrants` seedes ikke med data.
- Filerne er kun beregnet til lokal udvikling og test.
