-- Apply once to existing GamMaSite MySQL databases.
-- Event payment offers may repeat the same provider and offer type. Membership
-- offers remain unique per provider and offer type.
ALTER TABLE `PaymentOffers`
  DROP INDEX `UX_PaymentOffers_Scope_ContentItemId_Provider_OfferType`,
  MODIFY COLUMN `ContentItemScopeKey` int
    GENERATED ALWAYS AS (CASE WHEN `Scope` = 'MEMBERSHIP' THEN 0 ELSE NULL END) STORED,
  ADD UNIQUE KEY `UX_PaymentOffers_Scope_ContentItemId_Provider_OfferType`
    (`Scope`, `ContentItemScopeKey`, `Provider`, `OfferType`);
