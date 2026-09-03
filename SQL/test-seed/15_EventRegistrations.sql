SET NAMES utf8mb4;

DELETE FROM `EventRegistrations`;

INSERT INTO `EventRegistrations` (`ContentItemId`, `UserId`, `RegistrationType`, `Registered`, `ResponseText`, `Created`, `Updated`) VALUES
(
  (SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'bastard-fyraftenscafe-2026' LIMIT 1),
  (SELECT `Id` FROM `AspNetUsers` WHERE `Email` = 'bruger.test@gamma.local' LIMIT 1),
  'ATTENDEE',
  1,
  'Kommer forbi til en øl og et spil.',
  '2026-04-16 09:15:00',
  '2026-04-16 09:15:00'
),
(
  (SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'faglig-aften-juni-2026' LIMIT 1),
  (SELECT `Id` FROM `AspNetUsers` WHERE `Email` = 'admin.test@gamma.local' LIMIT 1),
  'ORGANIZER',
  1,
  'Hjælper med mad og oplægsholdere.',
  '2026-04-21 09:20:00',
  '2026-04-21 09:20:00'
),
(
  (SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'sommer-bingo-banko-fest-2026' LIMIT 1),
  (SELECT `Id` FROM `AspNetUsers` WHERE `Email` = 'bruger.test@gamma.local' LIMIT 1),
  'ATTENDEE',
  1,
  'Tager bankohumør med.',
  '2026-07-11 18:30:00',
  '2026-07-11 18:30:00'
),
(
  (SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'koebenhavnermuren-2026' LIMIT 1),
  (SELECT `Id` FROM `AspNetUsers` WHERE `Email` = 'admin.test@gamma.local' LIMIT 1),
  'ORGANIZER',
  1,
  'Koordinerer ruten.',
  '2026-08-16 10:15:00',
  '2026-08-16 10:15:00'
);
