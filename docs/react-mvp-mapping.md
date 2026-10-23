# React migration mapping

React er monteret på de kanoniske URL'er uden `/react`, og de mest brugte gamle GET-URL'er serverer nu samme React-host. Backend, database, ASP.NET Identity-login, cookies og roller er fortsat uændret.

| Eksisterende funktion | Kanonisk rute | Gammel GET-URL der nu viser React |
| --- | --- | --- |
| Forside | `/` | `/Home`, `/Home/Index` |
| Begivenheder | `/events` | `/Home/Arrangementer` |
| Eventdetalje | `/events/{slug}` | - |
| Nyheder | `/news` | - |
| Nyhedsdetalje | `/news/{slug}` | - |
| Medlemmer | `/members` | `/Users` |
| Kalender | `/calendar` | `/Calendar` |
| Bibliotek | `/library?path=...` | Bevares på `/Library` for fil-download-kompatibilitet |
| Betaling, produktliste | `/pay` | `/Pay`, `/Pay/Index` |
| Betalingsstatus | `/pay/success`, `/pay/cancel` | `/Pay/Success`, `/Pay/Cancel` |
| Admin events | `/admin/events` | - |
| Admin nyheder | `/admin/news` | - |
| Admin medlemmer/massebehandling | `/admin/users` | `/Users/Expanded`, `/Users/UpdateMass` |
| Admin roller | `/admin/roles` | `/Role`, `/Role/Create`, `/Role/Update/{id}` |
| Admin beskedmålgrupper | `/admin/messages` | `/Messages` |
| Admin tilmeldte | `/admin/events/{id}/registrations` | - |
| Email templates | `/admin/templates` | - |
| Betingelser | `/betingelser` | `/Home/Betingelser` |
| Cookies | `/cookies` | `/Home/Cookies` |

## Bevidst bevaret i Razor

- ASP.NET Identity login, register, logout, password reset, email confirmation, profil, password, email, 2FA og private data.
- Fil-downloads fra `/library?path=...`, når bibliotekselementet er en fil/blob.
- Eksisterende POST-actions, så gamle forms ikke er fjernet før React-flowene er endeligt godkendt.

## Ikke implementeret

- Magic login.
- JWT-login.
- Email-udsendelse/Mailgun. React-beskedsiden viser kun målgruppe-preview.
