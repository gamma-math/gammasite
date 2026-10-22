SET NAMES utf8mb4;

DELETE FROM `PaymentOffers`;

INSERT INTO `PaymentOffers` (
  `ContentItemId`, `Scope`, `Provider`, `OfferType`, `LinkLabel`, `ProductName`,
  `AmountMinor`, `Currency`, `BillingInterval`, `BillingIntervalCount`
) VALUES
(NULL, 'MEMBERSHIP', 'MOBILEPAY', 'ONE_TIME', 'Betal én gang', 'Kontingent', 15000, 'DKK', NULL, NULL),
(NULL, 'MEMBERSHIP', 'MOBILEPAY', 'RECURRING', 'Betal automatisk', 'Kontingent', 15000, 'DKK', 'YEAR', 1),
(NULL, 'MEMBERSHIP', 'STRIPE', 'ONE_TIME', 'Betal én gang', 'Kontingent', 15000, 'DKK', NULL, NULL),
(NULL, 'MEMBERSHIP', 'STRIPE', 'RECURRING', 'Betal automatisk', 'Kontingent', 15000, 'DKK', 'YEAR', 1);
