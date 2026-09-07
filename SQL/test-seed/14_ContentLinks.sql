SET NAMES utf8mb4;

DELETE FROM `ContentLinks`;

INSERT INTO `ContentLinks` (`ContentItemId`, `Label`, `Url`, `Type`, `SortOrder`, `Created`, `Updated`) VALUES
((SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'bastard-fyraftenscafe-2026' LIMIT 1), 'Facebook-event', 'https://www.facebook.com/events/935100782602251', 'FACEBOOK', 0, '2026-04-15 10:05:00', '2026-04-15 10:05:00'),
((SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'faglig-aften-juni-2026' LIMIT 1), 'Facebook-event', 'https://www.facebook.com/events/1352011586978094/', 'FACEBOOK', 0, '2026-04-20 10:05:00', '2026-04-20 10:05:00'),
((SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'sommer-bingo-banko-fest-2026' LIMIT 1), 'Facebook-event', 'https://facebook.com/events/s/gamma-sommer-bingo-banko-fest/1731919154816779/', 'FACEBOOK', 0, '2026-07-10 09:35:00', '2026-07-10 09:35:00'),
((SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'koebenhavnermuren-2026' LIMIT 1), 'Facebook-event', 'https://www.facebook.com/events/1528726255308131/', 'FACEBOOK', 0, '2026-08-15 10:05:00', '2026-08-15 10:05:00'),
((SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'koebenhavnermuren-2026' LIMIT 1), 'Tilmeld dig - betal 50 kr.', 'https://mobilepay.dk/erhverv/betalingslink/betalingslink-svar?phone=22766&amount=50&comment=K%C3%B8benhavnermuren', 'PAYMENT', 1, '2026-08-15 10:05:00', '2026-08-15 10:05:00'),
((SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'react-migrationen-er-i-gang' LIMIT 1), 'Læs projektstatus', 'https://github.com/gamma-math', 'OTHER', 0, '2026-08-26 08:50:00', '2026-08-26 08:50:00');
