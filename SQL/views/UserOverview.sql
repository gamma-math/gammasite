-- View definition for `UserOverview`
-- Use this in test or deployment setups where the view must be recreated.

SET NAMES utf8mb4;

DROP VIEW IF EXISTS `UserOverview`;

CREATE VIEW `UserOverview` AS
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
