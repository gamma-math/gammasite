# Test Seed

Denne mappe indeholder falske testdata til GAMMASITE.

Seed-filerne er lavet ud fra strukturen i produktionseksporten, men uden rigtige persondata eller prod-hemmeligheder.

## Kendte loginoplysninger

- Admin: `admin.test@gamma.local` / `Admin123!Test`
- Bruger: `bruger.test@gamma.local` / `Bruger123!Test`
- Oevrige testbrugere: `Bruger123!Test`

## Rekkefolge

Koer filerne i denne rekkefolge:

1. `01_AspNetRoles.sql`
2. `02_AspNetUsers.sql`
3. `03_AspNetUserRoles.sql`
4. `07_AspNetUserTokens.sql`

Du kan ogsaa koere `00_seed_all.sql`, hvis du vil koere det hele paa en gang.

## Bemaerkning

- `Keys`, `DeviceCodes` og `PersistedGrants` bliver ikke seeded med prod-data.
- `Keys` boer oprettes af testmiljoeet selv, saa vi ikke genbruger signeringsnoegler fra produktion.
- Filerne er kun beregnet til testdatabasen.
