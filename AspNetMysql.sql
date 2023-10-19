-- Reference: https://gist.github.com/jambelnet/1ea70236c933d644b36895b296fb44e5
--
-- MySql - ASP.NET Core Identity
--

--
-- Table structure for table `aspnetroles`
--

CREATE TABLE IF NOT EXISTS AspNetRoles (
  `Id` varchar(128) NOT NULL,
  `Name` varchar(256) NOT NULL,
  `NormalizedName` varchar(256) NOT NULL,
  `ConcurrencyStamp` varchar(256) DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

DROP TABLE IF EXISTS `AspNetRoleClaims`;
CREATE TABLE `AspNetRoleClaims` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `ClaimType` longtext,
  `ClaimValue` longtext,
  `RoleId` varchar(127) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_AspNetRoleClaims_RoleId` (`RoleId`)
);
-- --------------------------------------------------------

--
-- Table structure for table `aspnetuserclaims`
--

CREATE TABLE IF NOT EXISTS AspNetUserClaims (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `UserId` varchar(128) NOT NULL,
  `ClaimType` longtext,
  `ClaimValue` longtext,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Id` (`Id`),
  KEY `UserId` (`UserId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 AUTO_INCREMENT=1 ;

-- --------------------------------------------------------

--
-- Table structure for table `aspnetuserlogins`
--

CREATE TABLE IF NOT EXISTS AspNetUserLogins (
  `LoginProvider` varchar(128) NOT NULL,
  `ProviderKey` varchar(128) NOT NULL,
  `ProviderDisplayName` varchar(128) NOT NULL,
  `UserId` varchar(128) NOT NULL,
  PRIMARY KEY (`LoginProvider`,`ProviderKey`,`UserId`),
  KEY `ApplicationUser_Logins` (`UserId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Table structure for table `aspnetuserroles`
--

CREATE TABLE IF NOT EXISTS AspNetUserRoles (
  `UserId` varchar(128) NOT NULL,
  `RoleId` varchar(128) NOT NULL,
  PRIMARY KEY (`UserId`,`RoleId`),
  KEY `IdentityRole_Users` (`RoleId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Table structure for table `aspnetusers`
--

CREATE TABLE IF NOT EXISTS AspNetUsers (
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

CREATE TABLE IF NOT EXISTS AspNetUserTokens (
`UserId` varchar(128) NOT NULL,
`LoginProvider` varchar(127) NOT NULL,
Name varchar(127) NOT NULL,
Value longtext,
CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name)
);


CREATE TABLE IF NOT EXISTS Keys (
`Id` varchar(128) NOT NULL,
`Version` INTEGER NOT NULL,
`Created` text NOT NULL,
`Use` text NULL,
`Algorithm` text NOT NULL,
`IsX509Certificate` INTEGER NOT NULL,
`DataProtected` INTEGER NOT NULL,
`Data` longtext NOT NULL,
CONSTRAINT PK_Keys PRIMARY KEY (Id)
);

CREATE TABLE `PersistedGrants` (
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
    CONSTRAINT PK_PersistedGrants PRIMARY KEY (`Key`)
);

CREATE TABLE `DeviceCodes` (
`UserCode` varchar(128) NOT NULL,
`DeviceCode` TEXT NOT NULL,
`SubjectId` TEXT NULL,
`SessionId` TEXT NULL,
`ClientId` TEXT NOT NULL,
`Description` TEXT NULL,
`CreationTime` TEXT NOT NULL,
`Expiration` TEXT NOT NULL,
`Data` TEXT NOT NULL,
CONSTRAINT PK_DeviceCodes PRIMARY KEY (`UserCode`)
);

CREATE TABLE `__EFMigrationsHistory` (
  `MigrationId` text NOT NULL,
  `ProductVersion` text NOT NULL,
  PRIMARY KEY (`MigrationId`(255)));
--
-- Constraints for dumped tables
--

--
-- Constraints for table `aspnetuserclaims`
--
ALTER TABLE AspNetUserClaims
  ADD CONSTRAINT `ApplicationUser_Claims` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE ON UPDATE NO ACTION;

--
-- Constraints for table `aspnetuserlogins`
--
ALTER TABLE AspNetUserLogins
  ADD CONSTRAINT `ApplicationUser_Logins` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE ON UPDATE NO ACTION;

--
-- Constraints for table `aspnetuserroles`
--
ALTER TABLE AspNetUserRoles
  ADD CONSTRAINT `ApplicationUser_Roles` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE ON UPDATE NO ACTION,
  ADD CONSTRAINT `IdentityRole_Users` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE ON UPDATE NO ACTION;