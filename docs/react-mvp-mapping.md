# React migration mapping

React er monteret under `/react`, og de mest brugte gamle GET-URL'er serverer nu samme React-host. Backend, database, ASP.NET Identity-login, cookies og roller er fortsat uændret.

| Eksisterende funktion | React-rute | Gammel GET-URL der nu viser React |
| --- | --- | --- |
| Forside | `/react` | `/`, `/Home`, `/Home/Index` |
| Begivenheder | `/react/events` | `/Home/Arrangementer` |
| Eventdetalje | `/react/events/{slug}` | - |
| Nyheder | `/react/news` | - |
| Nyhedsdetalje | `/react/news/{slug}` | - |
| Medlemmer | `/react/members` | `/Users` |
| Kalender | `/react/calendar` | `/Calendar` |
| Bibliotek | `/react/library?path=...` | Bevares på `/Library` for fil-download-kompatibilitet |
| Betaling, produktliste | `/react/pay` | `/Pay`, `/Pay/Index` |
| Betaling, produktdetalje | `/react/pay/products/{id}` | `/Pay/Product/{id}` |
| Valgfri betaling | `/react/pay/generic` | `/Pay/Generisk` |
| Betalingsstatus | `/react/pay/success`, `/react/pay/cancel` | `/Pay/Success`, `/Pay/Cancel` |
| Admin events | `/react/admin/events` | - |
| Admin nyheder | `/react/admin/news` | - |
| Admin medlemmer/massebehandling | `/react/admin/users` | `/Users/Expanded`, `/Users/UpdateMass` |
| Admin roller | `/react/admin/roles` | `/Role`, `/Role/Create`, `/Role/Update/{id}` |
| Admin beskedmålgrupper | `/react/admin/messages` | `/Messages` |
| Admin tilmeldte | `/react/admin/events/{id}/registrations` | - |
| Email templates | `/react/admin/templates` | - |
| Betingelser | `/react/betingelser` | `/Home/Betingelser` |
| Cookies | `/react/cookies` | `/Home/Cookies` |

## Bevidst bevaret i Razor

- ASP.NET Identity login, register, logout, password reset, email confirmation, profil, password, email, 2FA og private data.
- Fil-downloads fra `/library?path=...`, når bibliotekselementet er en fil/blob.
- Eksisterende POST-actions, så gamle forms ikke er fjernet før React-flowene er endeligt godkendt.

## Ikke implementeret

- Magic login.
- JWT-login.
- Email-udsendelse/Mailgun. React-beskedsiden viser kun målgruppe-preview.
