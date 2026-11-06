-- GAMMA MySQL schema definition.
-- This file is the portable Source of Truth for schema comparison and deployment.

-- --------------------------------------------------------

--
-- Table structure for table `AspNetRoleClaims`
--

CREATE TABLE `AspNetRoleClaims` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ClaimType` longtext,
  `ClaimValue` longtext,
  `RoleId` varchar(127) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `AspNetRoles`
--

CREATE TABLE `AspNetRoles` (
  `Id` varchar(128) COLLATE utf8mb4_general_ci NOT NULL,
  `Name` varchar(256) COLLATE utf8mb4_general_ci NOT NULL,
  `NormalizedName` varchar(256) COLLATE utf8mb4_general_ci NOT NULL,
  `ConcurrencyStamp` varchar(256) COLLATE utf8mb4_general_ci DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `AspNetUserClaims`
--

CREATE TABLE `AspNetUserClaims` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` varchar(128) COLLATE utf8mb4_general_ci NOT NULL,
  `ClaimType` longtext COLLATE utf8mb4_general_ci,
  `ClaimValue` longtext COLLATE utf8mb4_general_ci
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `AspNetUserLogins`
--

CREATE TABLE `AspNetUserLogins` (
  `LoginProvider` varchar(128) COLLATE utf8mb4_general_ci NOT NULL,
  `ProviderKey` varchar(128) COLLATE utf8mb4_general_ci NOT NULL,
  `ProviderDisplayName` varchar(128) COLLATE utf8mb4_general_ci NOT NULL,
  `UserId` varchar(128) COLLATE utf8mb4_general_ci NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `AspNetUserRoles`
--

CREATE TABLE `AspNetUserRoles` (
  `UserId` varchar(128) COLLATE utf8mb4_general_ci NOT NULL,
  `RoleId` varchar(128) COLLATE utf8mb4_general_ci NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `AspNetUsers`
--

CREATE TABLE `AspNetUsers` (
  `Id` varchar(128) COLLATE utf8mb4_general_ci NOT NULL,
  `ConcurrencyStamp` varchar(256) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `Email` varchar(256) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `EmailConfirmed` tinyint(1) NOT NULL,
  `PasswordHash` longtext COLLATE utf8mb4_general_ci,
  `SecurityStamp` longtext COLLATE utf8mb4_general_ci,
  `PhoneNumber` longtext COLLATE utf8mb4_general_ci,
  `PhoneNumberConfirmed` tinyint(1) NOT NULL,
  `TwoFactorEnabled` tinyint(1) NOT NULL,
  `LockoutEndDateUtc` datetime DEFAULT NULL,
  `LockoutEnabled` tinyint(1) NOT NULL,
  `AccessFailedCount` int NOT NULL,
  `UserName` varchar(256) COLLATE utf8mb4_general_ci NOT NULL,
  `LockoutEnd` datetime DEFAULT NULL,
  `NormalizedEmail` varchar(256) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `NormalizedUserName` varchar(256) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `Navn` varchar(256) COLLATE utf8mb4_general_ci NOT NULL,
  `Adresse` mediumtext COLLATE utf8mb4_general_ci,
  `Status` int NOT NULL DEFAULT '0',
  `Visibility` int NOT NULL DEFAULT '1',
  `Aargang` int NOT NULL,
  `Beskaeftigelse` longtext COLLATE utf8mb4_general_ci,
  `KontingentDato` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `OprettetDato` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `AspNetUserTokens`
--

CREATE TABLE `AspNetUserTokens` (
  `UserId` varchar(128) NOT NULL,
  `LoginProvider` varchar(127) NOT NULL,
  `Name` varchar(127) NOT NULL,
  `Value` longtext
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `ContentItems`
--

CREATE TABLE `ContentItems` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Title` varchar(256) COLLATE utf8mb4_general_ci NOT NULL,
  `Slug` varchar(256) COLLATE utf8mb4_general_ci NOT NULL,
  `Summary` varchar(512) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `Body` longtext COLLATE utf8mb4_general_ci,
  `PictureUrl` varchar(1024) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `Tags` varchar(512) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `Type` varchar(32) COLLATE utf8mb4_general_ci NOT NULL,
  `Status` varchar(32) COLLATE utf8mb4_general_ci NOT NULL,
  `MaxParticipants` int DEFAULT NULL,
  `ShowOnFrontPage` tinyint NOT NULL DEFAULT '1',
  `StartDate` datetime DEFAULT NULL,
  `EndDate` datetime DEFAULT NULL,
  `RegistrationDeadline` datetime DEFAULT NULL,
  `Location` varchar(256) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `CreatedByUserId` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci DEFAULT NULL,
  `Created` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Updated` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `PublishedAt` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `ContentLinks`
--

CREATE TABLE `ContentLinks` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ContentItemId` int NOT NULL,
  `Label` varchar(128) NOT NULL,
  `Url` varchar(1024) NOT NULL,
  `Type` varchar(32) NOT NULL,
  `SortOrder` int NOT NULL DEFAULT '0',
  `Created` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Updated` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Table structure for table `DeviceCodes`
--

CREATE TABLE `DeviceCodes` (
  `UserCode` varchar(128) NOT NULL,
  `DeviceCode` text NOT NULL,
  `SubjectId` text,
  `SessionId` text,
  `ClientId` text NOT NULL,
  `Description` text,
  `CreationTime` text NOT NULL,
  `Expiration` text NOT NULL,
  `Data` text NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `EmailTemplates`
--

CREATE TABLE `EmailTemplates` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(128) NOT NULL,
  `Subject` varchar(256) NOT NULL,
  `Preheader` varchar(256) DEFAULT NULL,
  `HtmlBody` longtext NOT NULL,
  `TextBody` longtext,
  `TemplateType` varchar(32) NOT NULL,
  `IsActive` tinyint NOT NULL DEFAULT '1',
  `Created` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Updated` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Table structure for table `EventRegistrations`
--

CREATE TABLE `EventRegistrations` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ContentItemId` int NOT NULL,
  `UserId` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `RegistrationType` varchar(32) COLLATE utf8mb4_general_ci NOT NULL,
  `Registered` tinyint NOT NULL DEFAULT '1',
  `ResponseText` varchar(1024) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `Created` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Updated` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `Keys`
--

CREATE TABLE `Keys` (
  `Id` varchar(128) NOT NULL,
  `Version` int NOT NULL,
  `Created` text NOT NULL,
  `Use` text,
  `Algorithm` text NOT NULL,
  `IsX509Certificate` int NOT NULL,
  `DataProtected` int NOT NULL,
  `Data` longtext NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `PaymentOffers`
--

CREATE TABLE `PaymentOffers` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ContentItemId` int DEFAULT NULL,
  `ContentItemScopeKey` int GENERATED ALWAYS AS ((case when (`Scope` = _utf8mb4'MEMBERSHIP') then 0 else NULL end)) STORED,
  `Scope` varchar(32) NOT NULL DEFAULT 'EVENT',
  `Provider` varchar(32) NOT NULL,
  `OfferType` varchar(32) NOT NULL,
  `LinkLabel` varchar(128) DEFAULT NULL,
  `ProductName` varchar(256) NOT NULL,
  `ProviderProductId` varchar(128) DEFAULT NULL,
  `ProviderPriceId` varchar(128) DEFAULT NULL,
  `ProviderPaymentLinkId` varchar(128) DEFAULT NULL,
  `ProviderPaymentUrl` text,
  `AmountMinor` bigint DEFAULT NULL,
  `Currency` varchar(3) DEFAULT NULL,
  `BillingInterval` varchar(32) DEFAULT NULL,
  `BillingIntervalCount` int DEFAULT NULL,
  `IsActive` tinyint NOT NULL DEFAULT '1',
  `Created` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Updated` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Table structure for table `PaymentSubscriptions`
--

CREATE TABLE `PaymentSubscriptions` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `PaymentOfferId` int DEFAULT NULL,
  `Provider` varchar(32) COLLATE utf8mb4_general_ci NOT NULL,
  `ProviderSubscriptionId` varchar(128) COLLATE utf8mb4_general_ci NOT NULL,
  `ProductName` varchar(256) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `ProviderProductId` varchar(128) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `ProviderPriceId` varchar(128) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `Status` varchar(32) COLLATE utf8mb4_general_ci NOT NULL,
  `CurrentPeriodStart` datetime DEFAULT NULL,
  `CurrentPeriodEnd` datetime DEFAULT NULL,
  `NextChargeAt` datetime DEFAULT NULL,
  `LastPaidAt` datetime DEFAULT NULL,
  `CancelAtPeriodEnd` tinyint NOT NULL DEFAULT '0',
  `LastProcessedWebhookId` varchar(256) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `LastProcessedWebhookAt` datetime DEFAULT NULL,
  `Created` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Updated` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `Permissions`
--

CREATE TABLE `Permissions` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Code` varchar(128) NOT NULL,
  `Description` varchar(512) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Table structure for table `PersistedGrants`
--

CREATE TABLE `PersistedGrants` (
  `Key` varchar(128) NOT NULL,
  `Type` text NOT NULL,
  `SubjectId` text,
  `SessionId` text,
  `ClientId` text NOT NULL,
  `Description` text,
  `CreationTime` text NOT NULL,
  `Expiration` text,
  `ConsumedTime` text,
  `Data` text NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `RolePermissions`
--

CREATE TABLE `RolePermissions` (
  `RoleId` varchar(128) COLLATE utf8mb4_general_ci NOT NULL,
  `PermissionId` int NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `__EFMigrationsHistory`
--

CREATE TABLE `__EFMigrationsHistory` (
  `MigrationId` text NOT NULL,
  `ProductVersion` text NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

--
-- Indexes for dumped tables
--

--
-- Indexes for table `AspNetRoleClaims`
--
ALTER TABLE `AspNetRoleClaims`
  ADD PRIMARY KEY (`Id`),
  ADD KEY `IX_AspNetRoleClaims_RoleId` (`RoleId`);

--
-- Indexes for table `AspNetRoles`
--
ALTER TABLE `AspNetRoles`
  ADD PRIMARY KEY (`Id`);

--
-- Indexes for table `AspNetUserClaims`
--
ALTER TABLE `AspNetUserClaims`
  ADD PRIMARY KEY (`Id`),
  ADD UNIQUE KEY `Id` (`Id`),
  ADD KEY `UserId` (`UserId`);

--
-- Indexes for table `AspNetUserLogins`
--
ALTER TABLE `AspNetUserLogins`
  ADD PRIMARY KEY (`LoginProvider`,`ProviderKey`,`UserId`),
  ADD KEY `ApplicationUser_Logins` (`UserId`);

--
-- Indexes for table `AspNetUserRoles`
--
ALTER TABLE `AspNetUserRoles`
  ADD PRIMARY KEY (`UserId`,`RoleId`),
  ADD KEY `IdentityRole_Users` (`RoleId`);

--
-- Indexes for table `AspNetUsers`
--
ALTER TABLE `AspNetUsers`
  ADD PRIMARY KEY (`Id`);

--
-- Indexes for table `AspNetUserTokens`
--
ALTER TABLE `AspNetUserTokens`
  ADD PRIMARY KEY (`UserId`,`LoginProvider`,`Name`);

--
-- Indexes for table `ContentItems`
--
ALTER TABLE `ContentItems`
  ADD PRIMARY KEY (`Id`),
  ADD UNIQUE KEY `UX_ContentItems_Slug` (`Slug`),
  ADD KEY `IX_ContentItems_Type` (`Type`),
  ADD KEY `IX_ContentItems_Status` (`Status`),
  ADD KEY `IX_ContentItems_ShowOnFrontPage` (`ShowOnFrontPage`),
  ADD KEY `IX_ContentItems_StartDate` (`StartDate`),
  ADD KEY `IX_ContentItems_PublishedAt` (`PublishedAt`),
  ADD KEY `IX_ContentItems_CreatedByUserId` (`CreatedByUserId`);

--
-- Indexes for table `ContentLinks`
--
ALTER TABLE `ContentLinks`
  ADD PRIMARY KEY (`Id`),
  ADD KEY `IX_ContentLinks_ContentItemId` (`ContentItemId`);

--
-- Indexes for table `DeviceCodes`
--
ALTER TABLE `DeviceCodes`
  ADD PRIMARY KEY (`UserCode`);

--
-- Indexes for table `EmailTemplates`
--
ALTER TABLE `EmailTemplates`
  ADD PRIMARY KEY (`Id`),
  ADD KEY `IX_EmailTemplates_TemplateType` (`TemplateType`),
  ADD KEY `IX_EmailTemplates_IsActive` (`IsActive`);

--
-- Indexes for table `EventRegistrations`
--
ALTER TABLE `EventRegistrations`
  ADD PRIMARY KEY (`Id`),
  ADD UNIQUE KEY `UX_EventRegistrations_ContentItemId_UserId` (`ContentItemId`,`UserId`),
  ADD KEY `IX_EventRegistrations_ContentItemId` (`ContentItemId`),
  ADD KEY `IX_EventRegistrations_UserId` (`UserId`);

--
-- Indexes for table `Keys`
--
ALTER TABLE `Keys`
  ADD PRIMARY KEY (`Id`);

--
-- Indexes for table `PaymentOffers`
--
ALTER TABLE `PaymentOffers`
  ADD PRIMARY KEY (`Id`),
  ADD UNIQUE KEY `UX_PaymentOffers_Scope_ContentItemId_Provider_OfferType` (`Scope`,`ContentItemScopeKey`,`Provider`,`OfferType`),
  ADD KEY `IX_PaymentOffers_ContentItemId` (`ContentItemId`),
  ADD KEY `IX_PaymentOffers_ProviderPriceId` (`Provider`,`ProviderPriceId`);

--
-- Indexes for table `PaymentSubscriptions`
--
ALTER TABLE `PaymentSubscriptions`
  ADD PRIMARY KEY (`Id`),
  ADD UNIQUE KEY `UX_PaymentSubscriptions_UserId` (`UserId`),
  ADD UNIQUE KEY `UX_PaymentSubscriptions_ProviderSubscriptionId` (`Provider`,`ProviderSubscriptionId`),
  ADD KEY `IX_PaymentSubscriptions_Status` (`Status`),
  ADD KEY `IX_PaymentSubscriptions_ProviderPriceId` (`ProviderPriceId`),
  ADD KEY `IX_PaymentSubscriptions_PaymentOfferId` (`PaymentOfferId`);

--
-- Indexes for table `Permissions`
--
ALTER TABLE `Permissions`
  ADD PRIMARY KEY (`Id`),
  ADD UNIQUE KEY `UX_Permissions_Code` (`Code`);

--
-- Indexes for table `PersistedGrants`
--
ALTER TABLE `PersistedGrants`
  ADD PRIMARY KEY (`Key`);

--
-- Indexes for table `RolePermissions`
--
ALTER TABLE `RolePermissions`
  ADD PRIMARY KEY (`RoleId`,`PermissionId`),
  ADD KEY `IX_RolePermissions_PermissionId` (`PermissionId`);

--
-- Indexes for table `__EFMigrationsHistory`
--
ALTER TABLE `__EFMigrationsHistory`
  ADD PRIMARY KEY (`MigrationId`(255));

CREATE OR REPLACE VIEW `UserOverview` AS
SELECT
  `AspNetUsers`.`Id` AS `Id`,
  `AspNetUsers`.`Email` AS `Email`,
  `AspNetUsers`.`PhoneNumber` AS `PhoneNumber`,
  `AspNetUsers`.`Navn` AS `Navn`,
  `AspNetUsers`.`Aargang` AS `Aargang`,
  `AspNetUsers`.`Status` AS `Status`,
  `AspNetUsers`.`KontingentDato` AS `KontingentDato`,
  `AspNetUsers`.`Beskaeftigelse` AS `Beskaeftigelse`
FROM `AspNetUsers`;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `AspNetUserClaims`
--
ALTER TABLE `AspNetUserClaims`
  ADD CONSTRAINT `ApplicationUser_Claims` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE;

--
-- Constraints for table `AspNetUserLogins`
--
ALTER TABLE `AspNetUserLogins`
  ADD CONSTRAINT `ApplicationUser_Logins` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE;

--
-- Constraints for table `AspNetUserRoles`
--
ALTER TABLE `AspNetUserRoles`
  ADD CONSTRAINT `ApplicationUser_Roles` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
  ADD CONSTRAINT `IdentityRole_Users` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE;

--
-- Constraints for table `ContentItems`
--
ALTER TABLE `ContentItems`
  ADD CONSTRAINT `FK_ContentItems_AspNetUsers_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE SET NULL;

--
-- Constraints for table `ContentLinks`
--
ALTER TABLE `ContentLinks`
  ADD CONSTRAINT `FK_ContentLinks_ContentItems_ContentItemId` FOREIGN KEY (`ContentItemId`) REFERENCES `ContentItems` (`Id`) ON DELETE CASCADE;

--
-- Constraints for table `EventRegistrations`
--
ALTER TABLE `EventRegistrations`
  ADD CONSTRAINT `FK_EventRegistrations_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
  ADD CONSTRAINT `FK_EventRegistrations_ContentItems_ContentItemId` FOREIGN KEY (`ContentItemId`) REFERENCES `ContentItems` (`Id`) ON DELETE CASCADE;

--
-- Constraints for table `PaymentOffers`
--
ALTER TABLE `PaymentOffers`
  ADD CONSTRAINT `FK_PaymentOffers_ContentItems_ContentItemId` FOREIGN KEY (`ContentItemId`) REFERENCES `ContentItems` (`Id`) ON DELETE RESTRICT;

--
-- Constraints for table `PaymentSubscriptions`
--
ALTER TABLE `PaymentSubscriptions`
  ADD CONSTRAINT `FK_PaymentSubscriptions_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
  ADD CONSTRAINT `FK_PaymentSubscriptions_PaymentOffers_PaymentOfferId` FOREIGN KEY (`PaymentOfferId`) REFERENCES `PaymentOffers` (`Id`) ON DELETE RESTRICT;

--
-- Constraints for table `RolePermissions`
--
ALTER TABLE `RolePermissions`
  ADD CONSTRAINT `FK_RolePermissions_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE,
  ADD CONSTRAINT `FK_RolePermissions_Permissions_PermissionId` FOREIGN KEY (`PermissionId`) REFERENCES `Permissions` (`Id`) ON DELETE CASCADE;
