# MobilePay webhook-værktøj

`vipps.py` bruges til at administrere og kontrollere MobilePay-webhooks. Scriptet kalder MobilePay direkte; det er ikke en del af den offentlige GamMa-webside.

## Første opsætning

Kør kommandoerne fra repositoryets rod i PowerShell:

```powershell
Push-Location src/scripts/vipps
py -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
Copy-Item .env.example .env
Pop-Location
```

Åbn derefter `src/scripts/vipps/.env` og udfyld credentials. Scriptet læser kun denne lokale fil; den er ikke den samme som projektets `.env.local`.

Følgende værdier skal være sat:

```text
VIPPS_BASE_URL=https://apitest.vipps.no
VIPPS_MSN=...
VIPPS_CLIENT_ID=...
VIPPS_CLIENT_SECRET=...
VIPPS_OCP_APIM_SUBSCRIPTION_KEY=...
VIPPS_WEBHOOK_URL=https://test.gam-ma.dk/api/payments/vipps/webhook
```

Brug produktions-URL og produktionscredentials, når webhooken oprettes i produktion. Secrets må aldrig committes eller kopieres til dokumentation.

`VIPPS_MERCHANT_AGREEMENT_URL` bruges kun af `--create-agreement` til en manuel testaftale. Den er ikke nødvendig for webhook-kommandoerne.

## Kør kommandoerne med Docker

Der er en separat Docker Compose-service for hver kommando. Start kun den service, du vil bruge:

```powershell
Push-Location src/scripts/vipps
docker compose --profile manual run --rm webhooks
Pop-Location
```

De vigtigste services er:

| Service | Formål |
| --- | --- |
| `connection` | Kontrollér API-forbindelsen |
| `webhooks` | Vis registrerede webhooks |
| `register-webhook` | Opret en ny webhook |
| `rotate-webhook` | Opret og verificér en ny webhook uden at slette den gamle |
| `delete-webhook` | Slet webhooken angivet i `VIPPS_OLD_WEBHOOK_ID` |
| `payment`, `payment-status`, `payment-capture` | Test ePayment |
| `create-agreement`, `agreement-status`, `agreements`, `agreement-accept`, `agreement-stop` | Test Recurring-aftaler |
| `charge-status`, `charges` | Kontrollér Recurring-charges |

Eksempel på sikker rotation:

```powershell
Push-Location src/scripts/vipps
docker compose --profile manual run --rm rotate-webhook
# Deploy VIPPS_WEBHOOK_SECRET fra outputtet og test endpointet.
docker compose --profile manual run --rm delete-webhook
Pop-Location
```

Hver service kontrollerer sine nødvendige værdier fra `src/scripts/vipps/.env` og stopper med en forklaring, hvis en værdi mangler. ID'er til operationer sættes i `.env.example`-felterne `VIPPS_PAYMENT_REFERENCE`, `VIPPS_AGREEMENT_ID`, `VIPPS_CHARGE_ID` og `VIPPS_OLD_WEBHOOK_ID`.

## Se eksisterende webhooks

```powershell
src/scripts/vipps/.venv/Scripts/python.exe src/scripts/vipps/vipps.py --list-webhooks
```

Notér ID'et på den webhook, der skal udskiftes.

## Opret en ny webhook sikkert

Opret først den nye webhook, mens den gamle stadig er aktiv:

```powershell
src/scripts/vipps/.venv/Scripts/python.exe src/scripts/vipps/vipps.py `
  --rotate-webhook <gammelt-webhook-id> `
  https://test.gam-ma.dk/api/payments/vipps/webhook
```

Kommandoen opretter og verificerer den nye webhook, viser dens ID og udskriver den nye secret én gang. Den gamle webhook slettes ikke automatisk.

1. Gem den viste secret som `VIPPS_WEBHOOK_SECRET` i det korrekte GitHub Environment.
2. Deploy eller genstart GamMaSite i samme environment.
3. Gennemfør en testbetaling eller brug MobilePays delivery-test.
4. Kontrollér, at serveren svarer HTTP 2xx, og at betalingen eller betalingsaftalen opdateres.
5. Slet først derefter den gamle webhook:

```powershell
src/scripts/vipps/.venv/Scripts/python.exe src/scripts/vipps/vipps.py `
  --delete-webhook <gammelt-webhook-id>
```

## Direkte oprettelse og sletning

Hvis der ikke skal roteres fra en eksisterende webhook, kan en ny oprettes med:

```powershell
src/scripts/vipps/.venv/Scripts/python.exe src/scripts/vipps/vipps.py --register-webhook
```

Den viste secret skal gemmes som `VIPPS_WEBHOOK_SECRET`. Sletning kræver altid et eksplicit ID:

```powershell
src/scripts/vipps/.venv/Scripts/python.exe src/scripts/vipps/vipps.py --delete-webhook <webhook-id>
```

Brug aldrig en lokalhost-URL eller en Cloudflare-tunnel som permanent webhook-URL for test- eller produktionsserveren.
