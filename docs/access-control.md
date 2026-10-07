# Adgangsstyring

Adgangsstyringen kombinerer almindelig login-adgang, permissions og arrangørstatus.

## Grundregler

- Et almindeligt medlem behøver ingen permission for funktioner, som medlemmet allerede kunne bruge.
- `*.edit` giver adgang til at se og redigere hele området, inklusive oprettelse og sletning hvor det giver mening.
- Backend er den endelige adgangskontrol. Frontend skjuler kun funktioner, som brugeren ikke kan bruge.
- `ADMIN` beholder sine nuværende administrative muligheder. De fire permissions `content.edit`, `registrations.edit`, `messages.edit` og `roles.edit` kan ikke fjernes fra ADMIN.
- En arrangør kan redigere event og tilmeldinger for egne events uden en almindelig admin-permission.

## Sider og adgang

### Almindelige medlemsfunktioner

Medlemsoversigt, kalender, bibliotek, egne betalinger, egen finansoversigt og egen profil kræver fortsat kun login, hvor det allerede var kravet.

| Endpoint | Formål | Adgang |
| --- | --- | --- |
| `/react/members` | Medlemsoversigt med synlige oplysninger | Logget ind |
| `/react/calendar` | Se kommende events | Logget ind |
| `/react/bibliotek` | Se dokumenter og mapper | Logget ind |
| `/react/finance` | Se egne finansielle posteringer | Logget ind |
| `/react/account/manage` | Rediger egen profil | Logget ind |

Bibliotekets eksterne link til redigering af filer er fortsat ADMIN-specifikt.

### Eventtilmeldinger

| Endpoint | Formål | Adgang |
| --- | --- | --- |
| `/react/events/{slug}/registrations` | Se tilmeldinger | Logget ind |
| `/react/events/{slug}/registrations` | Tilføje, redigere eller fjerne andres tilmeldinger | `content.edit`, `registrations.edit` eller arrangør for det konkrete event |

En bruger kan altid administrere sin egen tilmelding efter de almindelige eventregler. Arrangøradgang gælder kun det konkrete event.

### Adminsider

| Endpoint | Formål | Adgang |
| --- | --- | --- |
| `/react/admin/events` | Se og administrere events | `content.edit` |
| `/react/admin/events/new` | Opret event | `content.edit` |
| `/react/admin/events/{id}/edit` | Rediger event | `content.edit` eller arrangør for eventet |
| `/react/admin/news` | Se og administrere nyheder | `content.edit` |
| `/react/admin/news/new` | Opret nyhed | `content.edit` |
| `/react/admin/news/{id}/edit` | Rediger nyhed | `content.edit` |
| `/react/admin/users` | Administrer medlemmer | `roles.edit` |
| `/react/admin/messages` | Vælg modtagere og send beskeder | `messages.edit` |
| `/react/admin/templates` | Administrer beskedskabeloner | `email_templates.edit` |
| `/react/admin/roles` | Administrer roller og permissions | `roles.edit` |
| `/react/admin/roles/new` | Opret rolle | `roles.edit` |
| `/react/admin/roles/{id}/edit` | Rediger rolle, medlemmer og permissions | `roles.edit` |
| `/react/admin/finance` | Finansielt adminoverblik | `finance.view.all` eller `finance.edit.all` |
| `/react/admin/finance/postings` | Se og eksportere alle posteringer | `finance.view.all` eller `finance.edit.all` |
| `/react/admin/finance/postings/{id}` | Se konkret postering | `finance.view.all` eller `finance.edit.all` |
| `/react/admin/finance/csv-import` | Importere finansdata | `finance.edit.all` |
| `/react/admin/finance/postings/edit` | Oprette og redigere posteringer | `finance.edit.all` |
| `/react/admin/finance/chart-of-accounts` | Administrere kontoplan | `finance.edit.all` |
| `/react/admin/finance/posting-groups` | Administrere posteringsgrupper | `finance.edit.all` |
| `/react/admin/finance/budgets` | Administrere budgetter | `finance.edit.all` |

## Permissions

| Permission | Formål |
| --- | --- |
| `content.edit` | Se, oprette, redigere, publicere og slette events og nyheder |
| `registrations.edit` | Tilføje, redigere og fjerne tilmeldinger til alle events |
| `messages.edit` | Se beskedområdet og sende beskeder |
| `email_templates.edit` | Se, oprette, redigere og slette beskedskabeloner |
| `roles.edit` | Administrere medlemmer, roller og permissions |
| `finance.view.all` | Se og eksportere alle finanser uden redigering |
| `finance.edit.all` | Se, oprette og redigere alle finansielle data |

`finance.edit.all` giver også adgang til finansområdets redigeringssider. Kontrol af, hvilke konkrete finansfelter der kan ændres, sker i finansfunktionaliteten.

## Rolleeksempler

| Rolle | Permissions |
| --- | --- |
| Arrangør | Ingen nødvendig permission for egne events; eventuel `content.edit` eller `registrations.edit` gælder alle events |
| Kommunikationsansvarlig | `messages.edit`, `email_templates.edit` |
| Revisor | `finance.view.all` |
| Kasserer | `finance.edit.all` |
| Administrator | Alle permissions |
