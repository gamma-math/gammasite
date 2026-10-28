# Testcheckliste for betalinger

Brug kun sandbox/testmiljøer og testbrugere. Sæt kryds efter hver gennemført test.

## Produkter og administration

- [x] Åbn `/admin/products`.
- [x] Kontrollér produkter for medlemskab: Stripe/MobilePay, engangsbetaling/gentagen betaling, beløb og frekvens.
- [x] Kontrollér at eventbetalinger kan oprettes og redigeres fra eventets administrationsside.
- [x] Kontrollér at almindelige links af typen `PAYMENT` stadig kan oprettes og vises som links.
- [x] Redigér beløbet på et medlemskabsprodukt, og kontrollér at den nye pris bruges ved næste betaling.

## Stripe – medlemskab

- [x] Gennemfør et medlemskab med kort/Apple Pay som engangsbetaling.
- [x] Kontrollér succes-siden, at brugeren står som betalt, og at `SYSTEM_PAYMENT_CONFIRMATION` sendes.
- [x] Gennemfør et medlemskab som gentagen betaling.
- [x] Kontrollér succes-siden, medlemskabsstatus og abonnementet på `/account/manage/subscriptions`.
- [o] Kontrollér at `SYSTEM_PAYMENT_RECURRING` sendes med produkt, beløb, betalingsfrekvens og kontolink.
- [x] Opsig abonnementet fra hjemmesiden, og kontrollér at det også er opsagt hos Stripe.
- [x] Test afbrudt betaling og kontrollér at brugeren ender på siden for betaling, der ikke blev gennemført.
- [o] Udløs eller simulér en senere Stripe-opkrævning, og kontrollér webhook, seneste betaling, næste opkrævning og email.

## MobilePay – medlemskab

- [x] Gennemfør et medlemskab med MobilePay som engangsbetaling.
- [x] Kontrollér succes-siden, medlemskabsstatus og `SYSTEM_PAYMENT_CONFIRMATION`.
- [] Find betalingen i MobilePay med:
  `docker compose -f src/services/vipps/docker-compose.yml --profile manual run --rm payment-status --status <reference>`
- [x] Gennemfør et medlemskab med MobilePay som gentagen betaling.
- [x] Kontrollér succes-siden, medlemskabsstatus, abonnementssiden og `SYSTEM_PAYMENT_RECURRING`.
- [x] Kontrollér aftalen i MobilePay med:
  `docker compose -f src/services/vipps/docker-compose.yml --profile manual run --rm agreement-status --agreement-status agr_TdWANbY`
- [x] Opsig aftalen fra hjemmesiden, og kontrollér status med:
  `docker compose -f src/services/vipps/docker-compose.yml --profile manual run --rm agreement-status --agreement-status agr_TdWANbY`
- [x] Kontrollér MobilePay-aftalens charges med:
  `docker compose -f src/services/vipps/docker-compose.yml --profile manual run --rm charges --list-charges agr_TdWANbY`.
- [] Kontrollér at næste MobilePay-charge oprettes automatisk til 1. februar det følgende år, og at en ny charge oprettes efter næste gennemførte betaling.
- [x] Test afbrudt betaling, og kontrollér at brugeren ender på siden for betaling, der ikke blev gennemført.

## Events

- [x] Opret et event med et Stripe-produkt og et MobilePay-produkt.
- [x] Gennemfør eventbetaling med Stripe, og kontrollér succes-side, deltagerstatus og betalingsbekræftelse.
- [x] Gennemfør eventbetaling med MobilePay, og kontrollér webhook, deltagerstatus og betalingsbekræftelse.
- [x] Afbryd en eventbetaling, og kontrollér at deltageren ikke markeres som betalt.
- [x] Kontrollér at betalingsproduktet vises sidst i eventets links og emails.

## Email og regression

- [x] Kontrollér at mailen indeholder brugerens navn og email, produkt, beløb, betalingssystem og dato.
- [x] Kontrollér at gentagne betalinger også viser betalingsfrekvens og link til kontoen.
- [x] Send samme webhook mere end én gang, og kontrollér at status og email ikke behandles dobbelt.
- [x] Kontrollér at login, events, nyheder, admin-sider og medlemsadgang stadig fungerer.

# FIXES

