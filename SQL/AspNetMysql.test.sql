-- Test variant for MySQL/phpMyAdmin import
-- Based on AspNetMysql.sql with MySQL-safe quoting for problematic identifiers.
-- Use charset: utf-8 when importing.

-- Reference: https://gist.github.com/jambelnet/1ea70236c933d644b36895b296fb44e5
--
-- MySql - ASP.NET Core Identity
--

--
-- Table structure for table `aspnetroles`
--

CREATE TABLE IF NOT EXISTS `AspNetRoles` (
  `Id` varchar(128) NOT NULL,
  `Name` varchar(256) NOT NULL,
  `NormalizedName` varchar(256) NOT NULL,
  `ConcurrencyStamp` varchar(256) DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `AspNetRoleClaims` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `ClaimType` longtext,
  `ClaimValue` longtext,
  `RoleId` varchar(127) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_AspNetRoleClaims_RoleId` (`RoleId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
-- --------------------------------------------------------

--
-- Table structure for table `aspnetuserclaims`
--

CREATE TABLE IF NOT EXISTS `AspNetUserClaims` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `UserId` varchar(128) NOT NULL,
  `ClaimType` longtext,
  `ClaimValue` longtext,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Id` (`Id`),
  KEY `UserId` (`UserId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 AUTO_INCREMENT=1;

-- --------------------------------------------------------

--
-- Table structure for table `aspnetuserlogins`
--

CREATE TABLE IF NOT EXISTS `AspNetUserLogins` (
  `LoginProvider` varchar(128) NOT NULL,
  `ProviderKey` varchar(128) NOT NULL,
  `ProviderDisplayName` varchar(128) NOT NULL,
  `UserId` varchar(128) NOT NULL,
  PRIMARY KEY (`LoginProvider`, `ProviderKey`, `UserId`),
  KEY `ApplicationUser_Logins` (`UserId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Table structure for table `aspnetuserroles`
--

CREATE TABLE IF NOT EXISTS `AspNetUserRoles` (
  `UserId` varchar(128) NOT NULL,
  `RoleId` varchar(128) NOT NULL,
  PRIMARY KEY (`UserId`, `RoleId`),
  KEY `IdentityRole_Users` (`RoleId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Table structure for table `aspnetusers`
--

CREATE TABLE IF NOT EXISTS `AspNetUsers` (
  `Id` varchar(128) NOT NULL,
  `ConcurrencyStamp` varchar(256) DEFAULT NULL,
  `Email` varchar(256) DEFAULT NULL,
  `EmailConfirmed` tinyint(1) NOT NULL,
  `PasswordHash` longtext,
  `SecurityStamp` longtext,
  `PhoneNumber` longtext,
  `PhoneNumberConfirmed` tinyint(1) NOT NULL,
  `TwoFactorEnabled` tinyint(1) NOT NULL,
  `LockoutEndDateUtc` datetime DEFAULT NULL,
  `LockoutEnabled` tinyint(1) NOT NULL,
  `AccessFailedCount` int(11) NOT NULL,
  `UserName` varchar(256) NOT NULL,
  `LockoutEnd` datetime DEFAULT NULL,
  `NormalizedEmail` varchar(256) DEFAULT NULL,
  `NormalizedUserName` varchar(256) DEFAULT NULL,
  `Navn` varchar(256) NOT NULL,
  `Adresse` mediumtext,
  `Status` int(11) NOT NULL DEFAULT '0',
  `Visibility` int(11) NOT NULL DEFAULT '1',
  `Aargang` int(11) NOT NULL,
  `Beskaeftigelse` longtext,
  `KontingentDato` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `OprettetDato` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Table structure for table `aspnetusertokens`
--

CREATE TABLE IF NOT EXISTS `AspNetUserTokens` (
  `UserId` varchar(128) NOT NULL,
  `LoginProvider` varchar(127) NOT NULL,
  `Name` varchar(127) NOT NULL,
  `Value` longtext,
  CONSTRAINT `PK_AspNetUserTokens` PRIMARY KEY (`UserId`, `LoginProvider`, `Name`)
);

CREATE TABLE IF NOT EXISTS `Keys` (
  `Id` varchar(128) NOT NULL,
  `Version` INTEGER NOT NULL,
  `Created` text NOT NULL,
  `Use` text NULL,
  `Algorithm` text NOT NULL,
  `IsX509Certificate` INTEGER NOT NULL,
  `DataProtected` INTEGER NOT NULL,
  `Data` longtext NOT NULL,
  CONSTRAINT `PK_Keys` PRIMARY KEY (`Id`)
);

CREATE TABLE IF NOT EXISTS `PersistedGrants` (
  `Key` varchar(128) NOT NULL,
  `Type` text NOT NULL,
  `SubjectId` text NULL,
  `SessionId` text NULL,
  `ClientId` text NOT NULL,
  `Description` text NULL,
  `CreationTime` text NOT NULL,
  `Expiration` text NULL,
  `ConsumedTime` text NULL,
  `Data` text NOT NULL,
  CONSTRAINT `PK_PersistedGrants` PRIMARY KEY (`Key`)
);

CREATE TABLE IF NOT EXISTS `DeviceCodes` (
  `UserCode` varchar(128) NOT NULL,
  `DeviceCode` TEXT NOT NULL,
  `SubjectId` TEXT NULL,
  `SessionId` TEXT NULL,
  `ClientId` TEXT NOT NULL,
  `Description` TEXT NULL,
  `CreationTime` TEXT NOT NULL,
  `Expiration` TEXT NOT NULL,
  `Data` TEXT NOT NULL,
  CONSTRAINT `PK_DeviceCodes` PRIMARY KEY (`UserCode`)
);

CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
  `MigrationId` text NOT NULL,
  `ProductVersion` text NOT NULL,
  PRIMARY KEY (`MigrationId`(255))
);

-- --------------------------------------------------------

--
-- Content tables for news, events, registrations and email templates
--

CREATE TABLE IF NOT EXISTS `ContentItems` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Title` varchar(256) NOT NULL,
  `Slug` varchar(256) NOT NULL,
  `Summary` varchar(512) DEFAULT NULL,
  `Body` longtext,
  `PictureUrl` varchar(1024) DEFAULT NULL,
  `Tags` varchar(512) DEFAULT NULL,
  `Type` varchar(32) NOT NULL,
  `Status` varchar(32) NOT NULL,
  `ShowOnFrontPage` tinyint(1) NOT NULL DEFAULT 1,
  `StartDate` datetime DEFAULT NULL,
  `EndDate` datetime DEFAULT NULL,
  `Location` varchar(256) DEFAULT NULL,
  `CreatedByUserId` varchar(128) DEFAULT NULL,
  `Created` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Updated` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `PublishedAt` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_ContentItems_Slug` (`Slug`),
  KEY `IX_ContentItems_Type` (`Type`),
  KEY `IX_ContentItems_Status` (`Status`),
  KEY `IX_ContentItems_ShowOnFrontPage` (`ShowOnFrontPage`),
  KEY `IX_ContentItems_StartDate` (`StartDate`),
  KEY `IX_ContentItems_PublishedAt` (`PublishedAt`),
  KEY `IX_ContentItems_CreatedByUserId` (`CreatedByUserId`),
  CONSTRAINT `FK_ContentItems_AspNetUsers_CreatedByUserId`
    FOREIGN KEY (`CreatedByUserId`) REFERENCES `AspNetUsers` (`Id`)
    ON DELETE SET NULL ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ContentLinks` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `ContentItemId` int(11) NOT NULL,
  `Label` varchar(128) NOT NULL,
  `Url` varchar(1024) NOT NULL,
  `Type` varchar(32) NOT NULL,
  `SortOrder` int(11) NOT NULL DEFAULT '0',
  `Created` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Updated` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  KEY `IX_ContentLinks_ContentItemId` (`ContentItemId`),
  CONSTRAINT `FK_ContentLinks_ContentItems_ContentItemId`
    FOREIGN KEY (`ContentItemId`) REFERENCES `ContentItems` (`Id`)
    ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `EventRegistrations` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `ContentItemId` int(11) NOT NULL,
  `UserId` varchar(128) NOT NULL,
  `RegistrationType` varchar(32) NOT NULL,
  `Registered` tinyint(1) NOT NULL DEFAULT '1',
  `ResponseText` varchar(1024) DEFAULT NULL,
  `Created` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Updated` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_EventRegistrations_ContentItemId_UserId` (`ContentItemId`, `UserId`),
  KEY `IX_EventRegistrations_ContentItemId` (`ContentItemId`),
  KEY `IX_EventRegistrations_UserId` (`UserId`),
  CONSTRAINT `FK_EventRegistrations_ContentItems_ContentItemId`
    FOREIGN KEY (`ContentItemId`) REFERENCES `ContentItems` (`Id`)
    ON DELETE CASCADE ON UPDATE NO ACTION,
  CONSTRAINT `FK_EventRegistrations_AspNetUsers_UserId`
    FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`)
    ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `EmailTemplates` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Name` varchar(128) NOT NULL,
  `Subject` varchar(256) NOT NULL,
  `Preheader` varchar(256) DEFAULT NULL,
  `HtmlBody` longtext NOT NULL,
  `TextBody` longtext,
  `TemplateType` varchar(32) NOT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `Created` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Updated` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  KEY `IX_EmailTemplates_TemplateType` (`TemplateType`),
  KEY `IX_EmailTemplates_IsActive` (`IsActive`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `aspnetuserclaims`
--
ALTER TABLE `AspNetUserClaims`
  ADD CONSTRAINT `ApplicationUser_Claims` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE ON UPDATE NO ACTION;

--
-- Constraints for table `aspnetuserlogins`
--
ALTER TABLE `AspNetUserLogins`
  ADD CONSTRAINT `ApplicationUser_Logins` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE ON UPDATE NO ACTION;

--
-- Constraints for table `aspnetuserroles`
--
ALTER TABLE `AspNetUserRoles`
  ADD CONSTRAINT `ApplicationUser_Roles` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE ON UPDATE NO ACTION,
  ADD CONSTRAINT `IdentityRole_Users` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE ON UPDATE NO ACTION;

