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
5. `13_ContentItems.sql`
6. `14_ContentLinks.sql`
7. `17_PaymentOffers.sql`
8. `15_EventRegistrations.sql`
9. `16_EmailTemplates.sql`
10. `18_AccessControlPermissions.sql` (permissions, role-permissions og role claims)

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
13. `13_ContentItems.sql`
14. `14_ContentLinks.sql`
15. `17_PaymentOffers.sql`
16. `15_EventRegistrations.sql`
17. `16_EmailTemplates.sql`
18. `18_AccessControlPermissions.sql`

Hvis du vil have de vigtigste testdata ind hurtigt, kan du koere `00_seed_all.sql`.

`00_seed_all.sql` rydder og seeder disse tabeller:

1. `AspNetRoles`
2. `AspNetUsers`
3. `AspNetUserRoles`
4. `AspNetUserTokens`
5. `ContentItems`
6. `ContentLinks`
7. `PaymentOffers`
8. `EventRegistrations`
9. `EmailTemplates`
10. `Permissions`
11. `RolePermissions`
12. `AspNetRoleClaims`

De nye content- og betalingstabeller kan ogsaa seedes separat med `13_ContentItems.sql`, `14_ContentLinks.sql`, `17_PaymentOffers.sql`, `15_EventRegistrations.sql` og `16_EmailTemplates.sql`, saa de kan koeres ovenpaa en eksisterende lokal Identity-database uden at overskrive brugere og roller.

## Bemaerkninger

- `Keys` holdes bevidst tom, saa testmiljoeet kan generere sine egne signeringsnoegler.
- `DeviceCodes` og `PersistedGrants` seedes ikke med data.
- Filerne er kun beregnet til lokal udvikling og test.
