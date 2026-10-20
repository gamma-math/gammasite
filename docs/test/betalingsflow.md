# Testcheckliste for betalinger

Brug kun sandbox/testmiljøer og testbrugere. Sæt kryds efter hver gennemført test.

## Forberedelse

- [x] Start GamMa lokalt med `run-local.ps1`.
- [x] Kontrollér, at testdatabasen er opdateret med betalingsprodukter og email-skabeloner.
- [x] Start Stripe CLI med forwarding til `/api/payments/stripe/webhook`.
- [x] Start MobilePay-tunnelen, og kontrollér at `VIPPS_WEBHOOK_URL` peger på den aktuelle tunnel.
- [x] Kontrollér, at MobilePay-webhooken findes:
  `scripts/vipps/.venv/Scripts/python.exe scripts/vipps/test_connection.py --list-webhooks`

## Produkter og administration

- [x] Åbn `/react/admin/products`.
- [x] Kontrollér produkter for medlemskab: Stripe/MobilePay, engangsbetaling/gentagen betaling, beløb og frekvens.
- [x] Kontrollér at eventbetalinger kan oprettes og redigeres fra eventets administrationsside.
- [x] Kontrollér at almindelige links af typen `PAYMENT` stadig kan oprettes og vises som links.
- [x] Redigér beløbet på et medlemskabsprodukt, og kontrollér at den nye pris bruges ved næste betaling.

## Stripe – medlemskab

- [x] Gennemfør et medlemskab med kort/Apple Pay som engangsbetaling.
- [x] Kontrollér succes-siden, at brugeren står som betalt, og at `SYSTEM_PAYMENT_CONFIRMATION` sendes.
- [x] Gennemfør et medlemskab som gentagen betaling.
- [x] Kontrollér succes-siden, medlemskabsstatus og abonnementet på `/react/account/manage/subscriptions`.
- [x] Kontrollér at `SYSTEM_PAYMENT_RECURRING` sendes med produkt, beløb, betalingsfrekvens og kontolink.
- [x] Opsig abonnementet fra hjemmesiden, og kontrollér at det også er opsagt hos Stripe.
- [x] Test afbrudt betaling og kontrollér at brugeren ender på siden for betaling, der ikke blev gennemført.
- [o] Udløs eller simulér en senere Stripe-opkrævning, og kontrollér webhook, seneste betaling, næste opkrævning og email.

## MobilePay – medlemskab

- [x] Gennemfør et medlemskab med MobilePay som engangsbetaling.
- [x] Kontrollér succes-siden, medlemskabsstatus og `SYSTEM_PAYMENT_CONFIRMATION`.
- [x] Find betalingen i MobilePay med:
  `scripts/vipps/.venv/Scripts/python.exe scripts/vipps/test_connection.py --status <reference>`
- [x] Gennemfør et medlemskab med MobilePay som gentagen betaling.
- [x] Kontrollér succes-siden, medlemskabsstatus, abonnementssiden og `SYSTEM_PAYMENT_RECURRING`.
- [x] Kontrollér aftalen i MobilePay med:
  `scripts/vipps/.venv/Scripts/python.exe scripts/vipps/test_connection.py --agreement-status agr_sWPvEGT`
- [x] Opsig aftalen fra hjemmesiden, og kontrollér status med `--agreement-status`.
- [x] Opret en testopkrævning med `scripts/vipps/.venv/Scripts/python.exe scripts/vipps/test_connection.py --create-charge agr_sWPvEGT`.
- [x] Kontrollér opkrævningen med `scripts/vipps/.venv/Scripts/python.exe scripts/vipps/test_connection.py --charge-status agr_sWPvEGT gamma-charge-cfdd8058a26a4960` samt webhook, datoer og email.
- [x] Test afbrudt betaling, og kontrollér at brugeren ender på siden for betaling, der ikke blev gennemført.

## Events

- [ ] Opret et event med et Stripe-produkt og et MobilePay-produkt.
- [ ] Gennemfør eventbetaling med Stripe, og kontrollér succes-side, deltagerstatus og betalingsbekræftelse.
- [ ] Gennemfør eventbetaling med MobilePay, og kontrollér webhook, deltagerstatus og betalingsbekræftelse.
- [ ] Afbryd en eventbetaling, og kontrollér at deltageren ikke markeres som betalt.
- [ ] Kontrollér at betalingsproduktet vises sidst i eventets links og emails.

## Email og regression

- [x] Kontrollér at mailen indeholder brugerens navn og email, produkt, beløb, betalingssystem og dato.
- [x] Kontrollér at gentagne betalinger også viser betalingsfrekvens og link til kontoen.
- [x] Send samme webhook mere end én gang, og kontrollér at status og email ikke behandles dobbelt.
- [ ] Kontrollér at login, events, nyheder, admin-sider og medlemsadgang stadig fungerer.
- [ ] Kør automatiske tests:
  `dotnet test src/test/GamMaSite.Tests/GamMaSite.Tests.csproj`
