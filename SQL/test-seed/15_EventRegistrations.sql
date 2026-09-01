SET NAMES utf8mb4;

DELETE FROM `EventRegistrations`;

INSERT INTO `EventRegistrations` (`ContentItemId`, `UserId`, `RegistrationType`, `Registered`, `ResponseText`, `Created`, `Updated`) VALUES
(
  (SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'karriere-efter-matematik' LIMIT 1),
  (SELECT `Id` FROM `AspNetUsers` WHERE `Email` = 'bruger.test@gamma.local' LIMIT 1),
  'ATTENDEE',
  1,
  'Jeg glæder mig til at høre om data science-sporet.',
  '2026-08-21 09:15:00',
  '2026-08-21 09:15:00'
),
(
  (SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'karriere-efter-matematik' LIMIT 1),
  (SELECT `Id` FROM `AspNetUsers` WHERE `Email` = 'admin.test@gamma.local' LIMIT 1),
  'HOST',
  1,
  'Tager imod oplægsholdere.',
  '2026-08-21 09:20:00',
  '2026-08-21 09:20:00'
),
(
  (SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'gamma-julebanko-2026' LIMIT 1),
  (SELECT `Id` FROM `AspNetUsers` WHERE `Email` = 'bruger.test@gamma.local' LIMIT 1),
  'ATTENDEE',
  1,
  'Kommer med godt bankohumør.',
  '2026-08-23 18:30:00',
  '2026-08-23 18:30:00'
);
