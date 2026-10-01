# Adgangsstyring

Dette dokument er første trin i arbejdet med rolle- og permissionsbaseret adgangsstyring. Det beskriver de eksisterende React-sider og den adgang, der bør bruges som udgangspunkt for de nye permissions.

## Begreber

- **Offentlig**: Kan ses uden login.
- **Logget ind**: Alle autentificerede brugere.
- **READ_ADMIN**: Kan læse udvalgte adminoversigter, men må ikke ændre indhold.
- **ADMIN**: Fuld administrativ adgang efter de eksisterende regler. Denne adgang skal bevares gennem de nye permissions.
- **Arrangør**: En bruger, der er registreret som arrangør på det konkrete arrangement. Arrangøradgang gælder kun dette arrangement og er ikke en almindelig rolle.

## Sider og adgang

Al funktionalitet, som et almindeligt logget ind medlem kan bruge i dag, skal fortsat være tilgængelig uden en særskilt permission. Det gælder eksempelvis medlemsoversigt, kalender, bibliotek, egne betalinger, egen finansoversigt og egen profil.

### Offentlige sider

| Endpoint | Formål | Adgang |
| --- | --- | --- |
| `/react` | Forside med aktuelle events og nyheder | Offentlig |
| `/react/events` | Oversigt over begivenheder | Offentlig |
| `/react/events/{slug}` | Detaljer om en begivenhed | Offentlig. Login kræves for tilmelding og for at se tilmeldte |
| `/react/news` | Oversigt over nyheder | Offentlig |
| `/react/news/{slug}` | Detaljer om en nyhed | Offentlig |
| `/react/om` | Information om GamMa | Offentlig |
| `/react/betingelser` | Betingelser og vedtægter | Offentlig |
| `/react/cookies` | Cookieinformation | Offentlig |

### Login, bruger og medlemsområde

| Endpoint | Formål | Adgang |
| --- | --- | --- |
| `/react/account/login` | Log ind | Offentlig |
| `/react/account/register` | Opret bruger | Offentlig |
| `/react/account/forgot-password` | Start nulstilling af password | Offentlig |
| `/react/account/resend-email-confirmation` | Gensend emailbekræftelse | Offentlig |
| `/react/account/manage` | Rediger egen profil | Logget ind |
| `/react/account/manage/email` | Skift egen email | Logget ind |
| `/react/account/manage/password` | Skift eget password | Logget ind |
| `/react/account/manage/two-factor` | Administrer egen to-faktor-login | Logget ind |
| `/react/account/manage/finance` | Se egne transaktioner | Logget ind |
| `/react/account/manage/personal-data` | Hent egne private data | Logget ind |
| `/react/account/manage/delete-personal-data` | Slet egen bruger/data | Logget ind |
| `/react/account/manage/logout` | Log ud | Logget ind |
| `/react/members` | Medlemsoversigt med de oplysninger, medlemmer har gjort synlige | Logget ind |
| `/react/calendar` | Se kommende begivenheder i kalenderform | Logget ind |
| `/react/bibliotek` | Se foreningens bibliotek og dokumenter | Logget ind |
| `/react/finance` | Finansoversigt og egne finansielle posteringer | Logget ind, alle brugere. Viser brugerens egne data |
| `/react/pay` | Betal medlemskab | Logget ind |
| `/react/pay/products/{id}` | Betal et konkret produkt/kontingent | Logget ind |
| `/react/pay/generic` | Overfør et valgfrit beløb | Logget ind |
| `/react/pay/success` | Vis gennemført betaling | Logget ind |
| `/react/pay/kontingent-success` | Vis gennemført kontingentbetaling | Logget ind |
| `/react/pay/cancel` | Vis annulleret betaling | Logget ind |

### Begivenhedstilmeldinger

| Endpoint | Formål | Adgang |
| --- | --- | --- |
| `/react/events/{slug}/registrations` | Se tilmeldinger til et konkret arrangement | Logget ind |
| `/react/events/{slug}/registrations` | Opret eller rediger tilmeldinger til arrangementet | Egen tilmelding: logget ind. Alle tilmeldinger: ADMIN i dag; fremover ADMIN eller arrangør for det konkrete arrangement |

Arrangøradgangen skal kontrolleres i backend ud fra brugerens `EventRegistration` med registration type `ORGANIZER` og det konkrete event. En arrangør må ikke dermed få adgang til andre arrangementers tilmeldinger.

### Adminsider

| Endpoint | Formål | Adgang |
| --- | --- | --- |
| `/react/admin/events` | Oversigt over alle events, inklusive ikke-publicerede | READ_ADMIN kan læse; ADMIN kan oprette, redigere og slette |
| `/react/admin/events/new` | Opret event | ADMIN i dag; fremover permission til events/nyheder |
| `/react/admin/events/{id}/edit` | Rediger event | ADMIN i dag; fremover permission til events/nyheder |
| `/react/admin/news` | Oversigt over alle nyheder, inklusive ikke-publicerede | READ_ADMIN kan læse; ADMIN kan oprette, redigere og slette |
| `/react/admin/news/new` | Opret nyhed | ADMIN i dag; fremover permission til events/nyheder |
| `/react/admin/news/{id}/edit` | Rediger nyhed | ADMIN i dag; fremover permission til events/nyheder |
| `/react/admin/users` | Fuldt medlemsregister og statusændringer | ADMIN |
| `/react/admin/messages` | Vælg modtagere, forhåndsvis og send beskeder | ADMIN |
| `/react/admin/templates` | Se og administrer email-/beskedskabeloner | ADMIN |
| `/react/admin/roles` | Se, oprette, redigere og slette roller samt tilknytte medlemmer | ADMIN |
| `/react/admin/roles/new` | Opret rolle | ADMIN |
| `/react/admin/roles/{id}/edit` | Rediger rolle og rolletilknytninger | ADMIN |
| `/react/admin/finance` | Samlet finansielt adminoverblik | ADMIN |
| `/react/admin/finance/postings` | Se alle finansielle posteringer | ADMIN |
| `/react/admin/finance/postings/edit` | Rediger og opret posteringer | ADMIN |
| `/react/admin/finance/postings/{id}` | Se/rediger en konkret postering | ADMIN |
| `/react/admin/finance/csv-import` | Importer bank- og MobilePay-filer | ADMIN |
| `/react/admin/finance/chart-of-accounts` | Administrer kontoplan | ADMIN |
| `/react/admin/finance/posting-groups` | Administrer posteringsgrupper | ADMIN |
| `/react/admin/finance/posting-groups/{id}` | Rediger en posteringsgruppe | ADMIN |
| `/react/admin/finance/budgets` | Administrer budgetter | ADMIN |
| `/react/admin/finance/budgets/{id}` | Rediger et budget | ADMIN |

De gamle adresser som `/Users`, `/Role`, `/Messages`, `/Pay` og `/Home/...` er aliaser eller kompatibilitetsadresser til ovenstående sider og skal have samme adgang.

## Forslag til permissions

Permissions bruges kun til funktioner ud over et almindeligt medlems adgang. Der bruges kun to niveauer pr. område:

- `*.view` betyder, at man kan se det udvidede/adminindhold.
- `*.edit` betyder, at man kan se og redigere hele området. Det inkluderer også at oprette og slette, hvor det giver mening.

Et almindeligt medlems adgang skal ikke have en permission. Backend skal være den endelige kontrol.

### Events og nyheder

- `content.view` – se ikke-publicerede events og nyheder i adminoversigterne.
- `content.edit` – se, oprette, redigere, publicere og slette events og nyheder. Events og nyheder holdes samlet.
- `registrations.edit.all` – redigere tilmeldinger til alle events.

En arrangør får adgang til at redigere tilmeldinger til egne events via arrangørstatus på eventet. Det er en kontekstregel og ikke en ekstra permission.

### Medlemmer

- `members.edit` – se alle medlemsoplysninger samt ændre medlemsstatus, herunder massebehandling.

Den almindelige medlemsoversigt kræver fortsat ingen permission.

### Beskeder og skabeloner

- `messages.edit` – se beskedområdet og sende beskeder.
- `email_templates.edit` – se, oprette, redigere og slette beskedskabeloner.

### Finans

- `finance.view.all` – se alle finanser, relevant for revisor.
- `finance.edit.all` – se og redigere alle finanser, herunder posteringer, budgetter, posteringsgrupper og import.

Kontoplanen skal ikke kunne redigeres af denne løsning. Den almindelige finansoversigt og egne posteringer kræver fortsat ingen permission.

### Roller og adgang

- `roles.edit` – se og administrere hele rolleområdet, herunder oprette, redigere, slette roller, tilknytte medlemmer og tildele permissions.

`ADMIN` bør fortsat have alle permissions. På den måde kan den nuværende administratoradgang bevares, mens eksempelvis en revisor, kasserer eller redaktør kan få en mindre adgang.

## Enkle rolleeksempler

| Rolle | Relevante permissions |
| --- | --- |
| Redaktør | `content.edit` |
| Arrangør | Arrangørstatus på egne events samt `registrations.edit.all` hvis personen skal redigere alle events |
| Medlemsadministrator | `members.edit` |
| Kommunikationsansvarlig | `messages.edit`, `email_templates.edit` |
| Revisor | `finance.view.all` |
| Kasserer | `finance.edit.all` |
| Administrator | Alle permissions |

## Backendområder, der skal ændres

De vigtigste eksisterende kontrolpunkter er:

- `ApiContentController`: indhold og eventtilmeldinger.
- `ApiFinanceController`: almindelig finansadgang og finans-admin.
- `ApiMembersController`: medlemsoversigt og medlemsstatus.
- `ApiMessagesController`: beskeder.
- `ApiEmailTemplatesController`: skabeloner.
- `ApiRolesController`: roller og medlemmer i roller.
- `ApiEditorController`: billedupload.

Frontend-menuer og knapper bør skjule funktioner uden den relevante permission, men alle ovenstående regler skal også håndhæves på backend-endpoints.
