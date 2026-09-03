-- Test seed for GAMMASITE
-- Uses fake test data only.

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS=0;

DELETE FROM `EventRegistrations`;
DELETE FROM `ContentLinks`;
DELETE FROM `ContentItems`;
DELETE FROM `EmailTemplates`;
DELETE FROM `AspNetUserTokens`;
DELETE FROM `AspNetUserRoles`;
DELETE FROM `AspNetUsers`;
DELETE FROM `AspNetRoles`;

INSERT INTO `AspNetRoles` (`Id`, `Name`, `NormalizedName`, `ConcurrencyStamp`) VALUES
('role-admin', 'Admin', 'ADMIN', '3bff48cf-bde8-43f2-8e4f-0d27d5f9ec01'),
('role-test', 'Test', 'TEST', '73f673e8-b29b-46dc-9bf1-8ca4a9422cc1'),
('role-finance', 'Finance', 'FINANCE', '793dddb1-a177-4a9a-9ff7-a2224f8533e1'),
('role-mail', 'Rolle mail test', 'ROLLE MAIL TEST', '2c2212b9-5c03-4f83-94fd-97769ad33dc4');

INSERT INTO `AspNetUsers` (
  `Id`, `ConcurrencyStamp`, `Email`, `EmailConfirmed`, `PasswordHash`, `SecurityStamp`,
  `PhoneNumber`, `PhoneNumberConfirmed`, `TwoFactorEnabled`, `LockoutEndDateUtc`,
  `LockoutEnabled`, `AccessFailedCount`, `UserName`, `LockoutEnd`, `NormalizedEmail`,
  `NormalizedUserName`, `Navn`, `Adresse`, `Status`, `Visibility`, `Aargang`,
  `Beskaeftigelse`, `KontingentDato`, `OprettetDato`
) VALUES
('user-admin-test', '6f87a0db-ef09-4f95-8dfd-70c93b03240e', 'admin.test@gamma.local', 1, 'AQAAAAIAAYagAAAAEJSqFi5XFfrmBQN7POTkg1Orn+pQ9VtPapw3cdtLNOekGyR19ltFlfT6Hq3GZAaMzw==', '0d7724d6-9c8c-45b5-bf28-e1f4647c6ce2', '12345678', 0, 0, NULL, 1, 0, 'admin.test@gamma.local', NULL, 'ADMIN.TEST@GAMMA.LOCAL', 'ADMIN.TEST@GAMMA.LOCAL', 'Test Admin', 'Testvej 1', 4, 1, 2024, 'Udvikler', UTC_TIMESTAMP(), UTC_TIMESTAMP()),
('user-member-test', 'c6f6d873-75dc-4dc8-8f28-dfc5b9e0e477', 'bruger.test@gamma.local', 1, 'AQAAAAIAAYagAAAAEP79bP/4vOktDMCCwxmL7/X8IbdIJ0xdeXjVsHyDb345QvrdYZT/89u8OtAYzWGTag==', '6dd40f35-b0f0-4bd5-ae73-1ce1b055b211', '87654321', 0, 0, NULL, 1, 0, 'bruger.test@gamma.local', NULL, 'BRUGER.TEST@GAMMA.LOCAL', 'BRUGER.TEST@GAMMA.LOCAL', 'Test Bruger', 'Brugervej 1', 0, 1, 2024, 'Studerende', UTC_TIMESTAMP(), UTC_TIMESTAMP()),
('user-fake-001', '11111111-1111-4111-8111-111111111111', 'sofie.test@gamma.local', 1, 'AQAAAAIAAYagAAAAEP79bP/4vOktDMCCwxmL7/X8IbdIJ0xdeXjVsHyDb345QvrdYZT/89u8OtAYzWGTag==', '11111111-aaaa-bbbb-cccc-111111111111', '20110001', 0, 0, NULL, 1, 0, 'sofie.test@gamma.local', NULL, 'SOFIE.TEST@GAMMA.LOCAL', 'SOFIE.TEST@GAMMA.LOCAL', 'Sofie Testsen', 'Eksempelvej 10', 1, 1, 2018, 'Analytiker', '2026-02-15 20:03:21', '2024-01-12 10:15:00'),
('user-fake-002', '22222222-2222-4222-8222-222222222222', 'mikkel.test@gamma.local', 1, 'AQAAAAIAAYagAAAAEP79bP/4vOktDMCCwxmL7/X8IbdIJ0xdeXjVsHyDb345QvrdYZT/89u8OtAYzWGTag==', '22222222-aaaa-bbbb-cccc-222222222222', '20110002', 0, 0, NULL, 1, 0, 'mikkel.test@gamma.local', NULL, 'MIKKEL.TEST@GAMMA.LOCAL', 'MIKKEL.TEST@GAMMA.LOCAL', 'Mikkel Testsen', 'Eksempelvej 11', 2, 1, 2016, 'Gymnasielaerer', '2025-11-01 08:00:00', '2024-01-13 11:00:00'),
('user-fake-003', '33333333-3333-4333-8333-333333333333', 'nora.test@gamma.local', 1, 'AQAAAAIAAYagAAAAEP79bP/4vOktDMCCwxmL7/X8IbdIJ0xdeXjVsHyDb345QvrdYZT/89u8OtAYzWGTag==', '33333333-aaaa-bbbb-cccc-333333333333', '20110003', 0, 0, NULL, 1, 0, 'nora.test@gamma.local', NULL, 'NORA.TEST@GAMMA.LOCAL', 'NORA.TEST@GAMMA.LOCAL', 'Nora Testsen', 'Eksempelvej 12', 1, 1, 2020, 'Studerende', '2026-01-10 12:30:00', '2024-02-01 09:00:00'),
('user-fake-004', '44444444-4444-4444-8444-444444444444', 'jonas.test@gamma.local', 1, 'AQAAAAIAAYagAAAAEP79bP/4vOktDMCCwxmL7/X8IbdIJ0xdeXjVsHyDb345QvrdYZT/89u8OtAYzWGTag==', '44444444-aaaa-bbbb-cccc-444444444444', '20110004', 0, 0, NULL, 1, 0, 'jonas.test@gamma.local', NULL, 'JONAS.TEST@GAMMA.LOCAL', 'JONAS.TEST@GAMMA.LOCAL', 'Jonas Testsen', 'Eksempelvej 13', 0, 1, 2023, 'Kandidatstuderende', '0001-01-01 00:00:00', '2024-03-02 13:45:00'),
('user-fake-005', '55555555-5555-4555-8555-555555555555', 'mathilde.test@gamma.local', 1, 'AQAAAAIAAYagAAAAEP79bP/4vOktDMCCwxmL7/X8IbdIJ0xdeXjVsHyDb345QvrdYZT/89u8OtAYzWGTag==', '55555555-aaaa-bbbb-cccc-555555555555', '20110005', 0, 0, NULL, 1, 0, 'mathilde.test@gamma.local', NULL, 'MATHILDE.TEST@GAMMA.LOCAL', 'MATHILDE.TEST@GAMMA.LOCAL', 'Mathilde Testsen', 'Eksempelvej 14', 4, 1, 2022, 'PhD-studerende', '2026-03-01 14:00:00', '2024-04-04 08:20:00'),
('user-fake-006', '66666666-6666-4666-8666-666666666666', 'rasmus.test@gamma.local', 1, 'AQAAAAIAAYagAAAAEP79bP/4vOktDMCCwxmL7/X8IbdIJ0xdeXjVsHyDb345QvrdYZT/89u8OtAYzWGTag==', '66666666-aaaa-bbbb-cccc-666666666666', '20110006', 0, 0, NULL, 1, 0, 'rasmus.test@gamma.local', NULL, 'RASMUS.TEST@GAMMA.LOCAL', 'RASMUS.TEST@GAMMA.LOCAL', 'Rasmus Testsen', 'Eksempelvej 15', 2, 0, 2014, 'Softwareudvikler', '2025-06-15 09:15:00', '2024-04-10 16:10:00'),
('user-fake-007', '77777777-7777-4777-8777-777777777777', 'camilla.test@gamma.local', 1, 'AQAAAAIAAYagAAAAEP79bP/4vOktDMCCwxmL7/X8IbdIJ0xdeXjVsHyDb345QvrdYZT/89u8OtAYzWGTag==', '77777777-aaaa-bbbb-cccc-777777777777', '20110007', 0, 0, NULL, 1, 0, 'camilla.test@gamma.local', NULL, 'CAMILLA.TEST@GAMMA.LOCAL', 'CAMILLA.TEST@GAMMA.LOCAL', 'Camilla Testsen', 'Eksempelvej 16', 1, 1, 2019, 'Data Scientist', '2026-02-20 17:10:00', '2024-05-12 12:00:00'),
('user-fake-008', '88888888-8888-4888-8888-888888888888', 'oliver.test@gamma.local', 1, 'AQAAAAIAAYagAAAAEP79bP/4vOktDMCCwxmL7/X8IbdIJ0xdeXjVsHyDb345QvrdYZT/89u8OtAYzWGTag==', '88888888-aaaa-bbbb-cccc-888888888888', '20110008', 0, 0, NULL, 1, 0, 'oliver.test@gamma.local', NULL, 'OLIVER.TEST@GAMMA.LOCAL', 'OLIVER.TEST@GAMMA.LOCAL', 'Oliver Testsen', 'Eksempelvej 17', 0, 1, 2025, 'Studerende', '0001-01-01 00:00:00', '2024-06-01 10:10:00'),
('user-fake-009', '99999999-9999-4999-8999-999999999999', 'amalie.test@gamma.local', 1, 'AQAAAAIAAYagAAAAEP79bP/4vOktDMCCwxmL7/X8IbdIJ0xdeXjVsHyDb345QvrdYZT/89u8OtAYzWGTag==', '99999999-aaaa-bbbb-cccc-999999999999', '20110009', 0, 0, NULL, 1, 0, 'amalie.test@gamma.local', NULL, 'AMALIE.TEST@GAMMA.LOCAL', 'AMALIE.TEST@GAMMA.LOCAL', 'Amalie Testsen', 'Eksempelvej 18', 1, 1, 2017, 'Aktuar', '2026-04-12 09:00:00', '2024-06-14 09:30:00'),
('user-fake-010', '10101010-1010-4010-8010-101010101010', 'frederik.test@gamma.local', 1, 'AQAAAAIAAYagAAAAEP79bP/4vOktDMCCwxmL7/X8IbdIJ0xdeXjVsHyDb345QvrdYZT/89u8OtAYzWGTag==', '10101010-aaaa-bbbb-cccc-101010101010', '20110010', 0, 0, NULL, 1, 0, 'frederik.test@gamma.local', NULL, 'FREDERIK.TEST@GAMMA.LOCAL', 'FREDERIK.TEST@GAMMA.LOCAL', 'Frederik Testsen', 'Eksempelvej 19', 2, 1, 2015, 'Konsulent', '2025-09-10 08:00:00', '2024-06-20 14:00:00'),
('user-fake-011', '11111110-1110-4110-8110-111111111110', 'ida.test@gamma.local', 1, 'AQAAAAIAAYagAAAAEP79bP/4vOktDMCCwxmL7/X8IbdIJ0xdeXjVsHyDb345QvrdYZT/89u8OtAYzWGTag==', '11111110-aaaa-bbbb-cccc-111111111110', '20110011', 0, 0, NULL, 1, 0, 'ida.test@gamma.local', NULL, 'IDA.TEST@GAMMA.LOCAL', 'IDA.TEST@GAMMA.LOCAL', 'Ida Testsen', 'Eksempelvej 20', 4, 1, 2026, 'Bachelorstuderende', '2026-05-01 12:00:00', '2024-07-01 11:20:00'),
('user-fake-012', '12121212-1212-4212-8212-121212121212', 'laura.test@gamma.local', 1, 'AQAAAAIAAYagAAAAEP79bP/4vOktDMCCwxmL7/X8IbdIJ0xdeXjVsHyDb345QvrdYZT/89u8OtAYzWGTag==', '12121212-aaaa-bbbb-cccc-121212121212', '20110012', 0, 0, NULL, 1, 0, 'laura.test@gamma.local', NULL, 'LAURA.TEST@GAMMA.LOCAL', 'LAURA.TEST@GAMMA.LOCAL', 'Laura Testsen', 'Eksempelvej 21', 1, 0, 2013, 'Forsker', '2026-01-22 15:45:00', '2024-07-08 10:05:00'),
('user-fake-013', '13131313-1313-4313-8313-131313131313', 'magnus.test@gamma.local', 1, 'AQAAAAIAAYagAAAAEP79bP/4vOktDMCCwxmL7/X8IbdIJ0xdeXjVsHyDb345QvrdYZT/89u8OtAYzWGTag==', '13131313-aaaa-bbbb-cccc-131313131313', '20110013', 0, 0, NULL, 1, 0, 'magnus.test@gamma.local', NULL, 'MAGNUS.TEST@GAMMA.LOCAL', 'MAGNUS.TEST@GAMMA.LOCAL', 'Magnus Testsen', 'Eksempelvej 22', 2, 1, 2012, 'Projektleder', '2025-05-30 13:15:00', '2024-07-15 09:45:00'),
('user-fake-014', '14141414-1414-4414-8414-141414141414', 'emma.test@gamma.local', 1, 'AQAAAAIAAYagAAAAEP79bP/4vOktDMCCwxmL7/X8IbdIJ0xdeXjVsHyDb345QvrdYZT/89u8OtAYzWGTag==', '14141414-aaaa-bbbb-cccc-141414141414', '20110014', 0, 0, NULL, 1, 0, 'emma.test@gamma.local', NULL, 'EMMA.TEST@GAMMA.LOCAL', 'EMMA.TEST@GAMMA.LOCAL', 'Emma Testsen', 'Eksempelvej 23', 4, 1, 2024, 'Kandidatstuderende', '2026-06-02 16:30:00', '2024-08-02 12:25:00'),
('user-fake-015', '15151515-1515-4515-8515-151515151515', 'victor.test@gamma.local', 1, 'AQAAAAIAAYagAAAAEP79bP/4vOktDMCCwxmL7/X8IbdIJ0xdeXjVsHyDb345QvrdYZT/89u8OtAYzWGTag==', '15151515-aaaa-bbbb-cccc-151515151515', '20110015', 0, 0, NULL, 1, 0, 'victor.test@gamma.local', NULL, 'VICTOR.TEST@GAMMA.LOCAL', 'VICTOR.TEST@GAMMA.LOCAL', 'Victor Testsen', 'Eksempelvej 24', 0, 1, 2027, 'Studerende', '0001-01-01 00:00:00', '2024-08-12 15:10:00'),
('user-fake-016', '16161616-1616-4616-8616-161616161616', 'anna.test@gamma.local', 1, 'AQAAAAIAAYagAAAAEP79bP/4vOktDMCCwxmL7/X8IbdIJ0xdeXjVsHyDb345QvrdYZT/89u8OtAYzWGTag==', '16161616-aaaa-bbbb-cccc-161616161616', '20110016', 0, 0, NULL, 1, 0, 'anna.test@gamma.local', NULL, 'ANNA.TEST@GAMMA.LOCAL', 'ANNA.TEST@GAMMA.LOCAL', 'Anna Testsen', 'Eksempelvej 25', 1, 1, 2011, 'Underviser', '2026-07-01 10:00:00', '2024-08-20 08:40:00'),
('user-fake-017', '17171717-1717-4717-8717-171717171717', 'william.test@gamma.local', 1, 'AQAAAAIAAYagAAAAEP79bP/4vOktDMCCwxmL7/X8IbdIJ0xdeXjVsHyDb345QvrdYZT/89u8OtAYzWGTag==', '17171717-aaaa-bbbb-cccc-171717171717', '20110017', 0, 0, NULL, 1, 0, 'william.test@gamma.local', NULL, 'WILLIAM.TEST@GAMMA.LOCAL', 'WILLIAM.TEST@GAMMA.LOCAL', 'William Testsen', 'Eksempelvej 26', 3, 0, 2010, 'Data Engineer', '2024-12-01 08:00:00', '2024-09-02 13:00:00'),
('user-fake-018', '18181818-1818-4818-8818-181818181818', 'clara.test@gamma.local', 1, 'AQAAAAIAAYagAAAAEP79bP/4vOktDMCCwxmL7/X8IbdIJ0xdeXjVsHyDb345QvrdYZT/89u8OtAYzWGTag==', '18181818-aaaa-bbbb-cccc-181818181818', '20110018', 0, 0, NULL, 1, 0, 'clara.test@gamma.local', NULL, 'CLARA.TEST@GAMMA.LOCAL', 'CLARA.TEST@GAMMA.LOCAL', 'Clara Testsen', 'Eksempelvej 27', 2, 1, 2018, 'BI-specialist', '2025-10-05 11:00:00', '2024-09-10 09:10:00');

INSERT INTO `AspNetUserTokens` (`UserId`, `LoginProvider`, `Name`, `Value`) VALUES
('user-admin-test', '[AspNetUserStore]', 'AuthenticatorKey', 'TESTADMINAUTHKEY0000000000000001'),
('user-member-test', '[AspNetUserStore]', 'AuthenticatorKey', 'TESTMEMBERAUTHKEY000000000000001'),
('user-fake-003', '[AspNetUserStore]', 'AuthenticatorKey', 'TESTUSERAUTHKEY000000000000000003'),
('user-fake-005', '[AspNetUserStore]', 'RecoveryCodes', 'code001;code002;code003;code004;code005');

INSERT INTO `AspNetUserRoles` (`UserId`, `RoleId`) VALUES
('user-admin-test', 'role-admin'),
('user-admin-test', 'role-finance'),
('user-member-test', 'role-test'),
('user-fake-001', 'role-admin'),
('user-fake-002', 'role-test'),
('user-fake-003', 'role-mail'),
('user-fake-005', 'role-finance');

INSERT INTO `ContentItems` (
  `Title`, `Slug`, `Summary`, `Body`, `PictureUrl`, `Tags`, `Type`, `Status`,
  `StartDate`, `EndDate`, `Location`, `CreatedByUserId`, `Created`, `Updated`, `PublishedAt`
) VALUES
(
  'Fyraftenscafe på Bastard',
  'bastard-fyraftenscafe-2026',
  'Vi gentager sidste års fyraftensøl på Bastard Cafe med hygge, snak og en god start på forsommeren.',
  'Onsdag den 13. maj kl. 16.00 gentager vi sidste års fyraftensøl på Bastard Cafe på Nørrebro. Vi booker borde, så vi kan sidde samlet, og der er en gratis sodavand eller øl til alle, der tilmelder sig som deltagere. Svar gerne senest den 1. maj.',
  'https://images.unsplash.com/photo-1519671482749-fd09be7ccebf?auto=format&fit=crop&w=1200&q=80',
  'event,bastard,fyraftenscafe',
  'EVENT',
  'PUBLISHED',
  '2026-05-13 16:00:00',
  '2026-05-13 19:00:00',
  'Bastard Cafe, Borgmestervangen 21, 2200 København N',
  'user-admin-test',
  '2026-04-15 10:00:00',
  '2026-04-15 10:00:00',
  '2026-04-15 10:00:00'
),
(
  'Faglig aften - 1. juni',
  'faglig-aften-juni-2026',
  'To færdiguddannede matematikere fortæller om emner, de brænder for.',
  'Mandag den 1. juni holder vi igen faglig aften. Flemming von Essen og Matias Lolk Andersen kommer og fortæller om emner, de brænder for, og GamMa sørger som sædvanlig for lidt mad og drikke. Arrangementet kræver tilmelding.',
  'https://images.unsplash.com/photo-1523580846011-d3a5bc25702b?auto=format&fit=crop&w=1200&q=80',
  'event,faglig-aften,alumner',
  'EVENT',
  'PUBLISHED',
  '2026-06-01 17:00:00',
  '2026-06-01 20:00:00',
  'HCØ, Universitetsparken 5, 2100 København Ø',
  'user-admin-test',
  '2026-04-20 10:00:00',
  '2026-04-20 10:00:00',
  '2026-04-20 10:00:00'
),
(
  'GamMa Sommer Bingo Banko Fest',
  'sommer-bingo-banko-fest-2026',
  'Sommerhygge, kongespil og bingo banko med fede præmier.',
  'Lørdag den 25. juli holder vi GamMa Sommer Bingo Banko Fest. Arrangementet starter kl. 12.00 med hygge, kongespil og gode vibes, og fra kl. 13.30 spiller vi bingo banko med fede præmier. Familier og partnere er velkomne.',
  'https://images.unsplash.com/photo-1507525428034-b723cf961d3e?auto=format&fit=crop&w=1200&q=80',
  'event,sommer,banko',
  'EVENT',
  'PUBLISHED',
  '2026-07-25 12:00:00',
  '2026-07-25 18:00:00',
  'ØB, Østerbrogade 74',
  'user-admin-test',
  '2026-07-10 09:30:00',
  '2026-07-10 09:30:00',
  '2026-07-10 09:30:00'
),
(
  'Københavnermuren',
  'koebenhavnermuren-2026',
  'En hyggelig dag med København, vand, mad og gode mennesker.',
  'Lørdag den 5. september tager vi på Københavnermuren. Det bliver en anderledes GamMa-dag med København, vand, mad og gode mennesker. Resten holder vi lige lidt hemmeligt.',
  'https://images.unsplash.com/photo-1513622470522-26c3c8a854bc?auto=format&fit=crop&w=1200&q=80',
  'event,koebenhavn,tur',
  'EVENT',
  'PUBLISHED',
  '2026-09-05 11:00:00',
  '2026-09-05 17:00:00',
  'København',
  'user-admin-test',
  '2026-08-15 10:00:00',
  '2026-08-15 10:00:00',
  '2026-08-15 10:00:00'
),
(
  'Professoraften',
  'professoraften-2026',
  'En faglig aften med oplæg fra professorer og god videnskab.',
  'Vi runder året af med professoraften: en faglig aften med oplæg fra professorer, god videnskab og tid til spørgsmål og samtaler på tværs af årgange.',
  'https://images.unsplash.com/photo-1532094349884-543bc11b234d?auto=format&fit=crop&w=1200&q=80',
  'event,professoraften,faglig-aften',
  'EVENT',
  'PUBLISHED',
  '2026-12-02 17:00:00',
  '2026-12-02 20:00:00',
  'Universitetsparken',
  'user-admin-test',
  '2026-09-01 12:00:00',
  '2026-09-01 12:00:00',
  '2026-09-01 12:00:00'
),
(
  'Nyt backendlag til events og nyheder',
  'nyt-backendlag-events-nyheder',
  'GAMMASITE har fået nye tabeller og API’er til content, tilmeldinger og email templates.',
  'Det nye backendlag gør det muligt at bygge React-frontenden ovenpå eksisterende login, roller og MySQL-database uden at ændre Identity-strukturen.',
  'https://images.unsplash.com/photo-1516321318423-f06f85e504b3?auto=format&fit=crop&w=1200&q=80',
  'nyhed,backend,react',
  'NEWS',
  'PUBLISHED',
  NULL,
  NULL,
  NULL,
  'user-admin-test',
  '2026-08-18 14:20:00',
  '2026-08-18 14:20:00',
  '2026-08-18 14:20:00'
),
(
  'React-migrationen er i gang',
  'react-migrationen-er-i-gang',
  'De første almindelige sider findes nu som React-sider ovenpå ASP.NET Core API’er.',
  'Migrationen bevarer ASP.NET Identity-login, cookies og roller, mens frontend gradvist flyttes til React.',
  'https://images.unsplash.com/photo-1555066931-4365d14bab8c?auto=format&fit=crop&w=1200&q=80',
  'nyhed,react,migration',
  'NEWS',
  'PUBLISHED',
  NULL,
  NULL,
  NULL,
  'user-admin-test',
  '2026-08-26 08:45:00',
  '2026-08-26 08:45:00',
  '2026-08-26 08:45:00'
),
(
  'Bestyrelsen søger input til 2027',
  'bestyrelsen-soeger-input-2027',
  'Har du ideer til arrangementer, formater eller samarbejder, så vil vi gerne høre fra dig.',
  'Send gerne forslag til bestyrelsen. Vi samler input til næste års program og prioriterer events, der binder studerende og alumner tættere sammen.',
  'https://images.unsplash.com/photo-1552664730-d307ca884978?auto=format&fit=crop&w=1200&q=80',
  'nyhed,bestyrelse,input',
  'NEWS',
  'DRAFT',
  NULL,
  NULL,
  NULL,
  'user-admin-test',
  '2026-08-28 16:10:00',
  '2026-08-28 16:10:00',
  NULL
);

INSERT INTO `ContentLinks` (`ContentItemId`, `Label`, `Url`, `Type`, `SortOrder`, `Created`, `Updated`) VALUES
((SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'bastard-fyraftenscafe-2026' LIMIT 1), 'Facebook-event', 'https://www.facebook.com/events/935100782602251', 'FACEBOOK', 0, '2026-04-15 10:05:00', '2026-04-15 10:05:00'),
((SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'faglig-aften-juni-2026' LIMIT 1), 'Facebook-event', 'https://www.facebook.com/events/1352011586978094/', 'FACEBOOK', 0, '2026-04-20 10:05:00', '2026-04-20 10:05:00'),
((SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'sommer-bingo-banko-fest-2026' LIMIT 1), 'Facebook-event', 'https://facebook.com/events/s/gamma-sommer-bingo-banko-fest/1731919154816779/', 'FACEBOOK', 0, '2026-07-10 09:35:00', '2026-07-10 09:35:00'),
((SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'koebenhavnermuren-2026' LIMIT 1), 'Facebook-event', 'https://www.facebook.com/events/1528726255308131/', 'FACEBOOK', 0, '2026-08-15 10:05:00', '2026-08-15 10:05:00'),
((SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'koebenhavnermuren-2026' LIMIT 1), 'Tilmeld dig - betal 50 kr.', 'https://mobilepay.dk/erhverv/betalingslink/betalingslink-svar?phone=22766&amount=50&comment=K%C3%B8benhavnermuren', 'PAYMENT', 1, '2026-08-15 10:05:00', '2026-08-15 10:05:00'),
((SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'react-migrationen-er-i-gang' LIMIT 1), 'Læs projektstatus', 'https://github.com/gamma-math', 'OTHER', 0, '2026-08-26 08:50:00', '2026-08-26 08:50:00');

INSERT INTO `EventRegistrations` (`ContentItemId`, `UserId`, `RegistrationType`, `Registered`, `ResponseText`, `Created`, `Updated`) VALUES
((SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'bastard-fyraftenscafe-2026' LIMIT 1), 'user-member-test', 'ATTENDEE', 1, 'Kommer forbi til en øl og et spil.', '2026-04-16 09:15:00', '2026-04-16 09:15:00'),
((SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'faglig-aften-juni-2026' LIMIT 1), 'user-admin-test', 'ORGANIZER', 1, 'Hjælper med mad og oplægsholdere.', '2026-04-21 09:20:00', '2026-04-21 09:20:00'),
((SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'sommer-bingo-banko-fest-2026' LIMIT 1), 'user-member-test', 'ATTENDEE', 1, 'Tager bankohumør med.', '2026-07-11 18:30:00', '2026-07-11 18:30:00'),
((SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'koebenhavnermuren-2026' LIMIT 1), 'user-admin-test', 'ORGANIZER', 1, 'Koordinerer ruten.', '2026-08-16 10:15:00', '2026-08-16 10:15:00');

DELETE FROM `EmailTemplates`;

INSERT INTO `EmailTemplates` (`Name`, `Subject`, `Preheader`, `HtmlBody`, `TextBody`, `TemplateType`, `IsActive`, `Created`, `Updated`) VALUES
(
  'GamMa Summermail Template',
  'Sommernyt fra GamMa',
  'Sommernyt, kommende arrangementer og nyheder fra GamMa',
  '<!-- GammaEmailBlockDesign eventColor="#18a999" newsColor="#1877F2" -->
<!--
  GamMa summer mail template
  Rediger primært indholdet mellem:
  START: CONTENT
  END: CONTENT

  Arbejdsgang:
  1. Opdater preheader og intro.
  2. Tilpas eller kopiér indholdsblokkene efter behov.
  3. Udskift links, arrangører og eventuelle billeder.
-->

<!-- Preheader (hidden in most clients) -->
<div style="display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;">
  [Kort preheader til sommermailen]
</div>

<!-- Outer wrapper -->
<div style="margin:0;padding:0;background-color:#dff5f2;width:100%;font-family:Arial,sans-serif;">

  <!-- Banner -->
  <a href="https://gam-ma.dk/" target="_blank" style="text-decoration:none;display:block;">
    <img src="https://github.com/AbrahimBorgiPrivat/GAMMA---ASSETS/blob/main/res/mails/img/headers/GamMa_Sommer_Bjaelke.jpg?raw=true"
         alt="GamMa sommer banner"
         width="100%"
         style="display:block;border:0;outline:none;text-decoration:none;height:auto;">
  </a>

  <!-- MAIN CONTAINER -->
  <div style="width:90%;max-width:720px;margin:16px auto 24px auto;background-color:#ffffff;
              padding:20px;color:#173042;line-height:1.55;font-size:0.95rem;
              box-shadow:0 6px 22px rgba(20,70,90,0.12);">

    <div style="text-align:center;font-size:18px;letter-spacing:4px;color:#78b7b0;margin:0 0 10px;">SUMMER VIBES</div>

    <p style="margin:0 0 12px;"><strong>K&aelig;re GamMa-medlem,</strong></p>

    <p style="margin:0 0 18px;">
      [Kort sommerlig intro til mailen. Skriv her, hvad mailen handler om, og hvorfor man skal l&aelig;se med.]
    </p>

    <!-- START: CONTENT -->

{{ContentBlocks}}

<!-- END: CONTENT -->

    <p style="margin:18px 0 0;">
      Vi h&aring;ber, du har lyst til at kigge forbi og tage en ven med, hvis du kender en GamMa''er, der ogs&aring; b&oslash;r f&aring; det med.
    </p>

    <p style="margin:8px 0 0;">
      Med venlig hilsen<br>
      <strong>GamMas bestyrelse</strong>
    </p>

    <div style="text-align:center;font-size:18px;letter-spacing:4px;color:#78b7b0;margin:10px 0 0;">SUN AND MATH</div>
  </div>

  <div style="height:1px;background-color:#cfe6e2;width:90%;max-width:720px;margin:0 auto 12px auto;"></div>

  <!-- Membership -->
  <div style="width:90%;max-width:720px;margin:0 auto 24px auto;color:#173042;line-height:1.45;font-size:0.9rem;">
    <div style="padding:16px;background-color:#fffefb;border-radius:10px;border:1px solid #e9efe8;">
      <p style="margin:0 0 8px;font-weight:bold;">Medlemskab?</p>
      <p style="margin:0 0 8px;">Husk, at deltagelse i GamMa-arrangementer kr&aelig;ver et aktivt medlemskab.</p>
      <p style="margin:0 0 8px;">
        Tjek betaling og opdater dine oplysninger p&aring;
        <a href="https://www.gam-ma.dk/Identity/Account/Manage" target="_blank" style="color:#337ab7;text-decoration:none;">
          GamMa.dk - <span style="font-weight:bold;">Profil</span>
        </a>.
      </p>
      <p style="margin:0 0 8px;">Bliv medlem s&aring;dan her:</p>
      <ul style="margin:0 0 10px;padding-left:20px;">
        <li>MobilePay: <strong>150 kr. til 22766</strong></li>
        <li>Log ind p&aring; <a href="https://gam-ma.dk/" style="color:#337ab7;text-decoration:none;">gam-ma.dk</a> og betal med kort under <em>Menu &gt;&gt; Betal</em></li>
        <li>Skan eller tryk p&aring; QR-koden nedenfor</li>
      </ul>
      <p style="margin:0 0 10px;font-size:0.85rem;color:#5b7383;">
        Er du studerende, er medlemskabet gratis. Kontakt bestyrelsen, s&aring; vi kan notere det.
      </p>
      <p style="margin:0 0 10px;text-align:center;">
        <a href="https://mobilepay.dk/erhverv/betalingslink/betalingslink-svar?phone=22766&amp;amount=150&amp;comment=Medlemskab" target="_blank" style="text-decoration:none;">
          <img src="https://github.com/AbrahimBorgiPrivat/GAMMA---ASSETS/blob/main/res/mails/img/qr/QR_medlemskab.png?raw=true"
               alt="Medlemskab MobilePay QR"
               width="150"
               style="border:0;display:block;margin:0 auto;height:auto;">
        </a>
      </p>
      <p style="margin:0;text-align:center;font-size:0.75rem;">
        &Oslash;nsker du at blive afmeldt eller udmeldt, s&aring; log ind p&aring;
        <a href="https://www.gam-ma.dk/Identity/Account/Manage" target="_blank" style="color:#337ab7;text-decoration:none;">GamMa.dk - Privatdata</a>
        og klik <b>''slet''</b>.
      </p>
    </div>
  </div>

  <div style="height:1px;background-color:#cfe6e2;width:90%;max-width:720px;margin:0 auto 12px auto;"></div>

  <!-- Social icons -->
  <div style="width:90%;max-width:720px;margin:0 auto 24px auto;text-align:center;background-color:#ffffff;border-radius:10px;">
    <table role="presentation" cellpadding="0" cellspacing="0" style="width:100%;border-collapse:collapse;border:none;background:none;">
      <tbody>
        <tr>
          <td style="padding:10px;border:none;background:none;text-align:center;">
            <a href="https://www.linkedin.com/company/gamma-math-ucph/" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;">
              <img src="https://cdn-icons-png.flaticon.com/512/174/174857.png" alt="LinkedIn" width="40" style="display:block;margin:0 auto;border:0;height:auto;">
              <div style="margin-top:6px;font-size:0.7rem;color:#000000;text-align:center;">LinkedIn</div>
            </a>
          </td>
          <td style="padding:10px;border:none;background:none;text-align:center;">
            <a href="https://www.instagram.com/gamma_ku_2100?igsh=MTFjYmk4M213djc0NA==" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;">
              <img src="https://cdn-icons-png.flaticon.com/512/2111/2111463.png" alt="Instagram" width="40" style="display:block;margin:0 auto;border:0;height:auto;">
              <div style="margin-top:6px;font-size:0.7rem;color:#000000;text-align:center;">Instagram</div>
            </a>
          </td>
          <td style="padding:10px;border:none;background:none;text-align:center;">
            <a href="https://www.facebook.com/share/g/1Exv7epudc/?mibextid=wwXIfr" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;">
              <img src="https://cdn-icons-png.flaticon.com/512/733/733547.png" alt="Facebook" width="40" style="display:block;margin:0 auto;border:0;height:auto;">
              <div style="margin-top:6px;font-size:0.7rem;color:#000000;text-align:center;">Facebook</div>
            </a>
          </td>
        </tr>
      </tbody>
    </table>
  </div>

  <div style="height:16px;"></div>
</div>
',
  'Sommernyt fra GamMa
{{ContentBlocks}}',
  'NEWSLETTER',
  1,
  '2026-08-26 09:00:00',
  '2026-08-26 09:00:00'
),
(
  'GamMa Event Template',
  'Kommende arrangementer fra GamMa',
  'Kommende arrangementer fra GamMa',
  '<!-- GammaEmailBlockDesign eventColor="#1f78c1" newsColor="#1877F2" -->
<!--
  GamMa event-mail template
  Rediger kun indholdet mellem:
  START: EVENTS
  END: EVENTS

  Arbejdsgang:
  1. Kopiér en event-blok for hvert arrangement.
  2. Udskift titel, brødtekst, knapper og links.
  3. Slet blokke, du ikke skal bruge.
-->

<!-- Preheader (hidden in most clients) -->
<div style="display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;">
  Kommende arrangementer fra GamMa
</div>

<!-- Outer wrapper -->
<div style="margin:0;padding:0;background-color:#eaf2fb;width:100%;font-family:Arial,sans-serif;">

  <!-- Banner -->
  <a href="https://gam-ma.dk/" target="_blank" style="text-decoration:none;display:block;">
    <img src="https://gam-ma.dk/lib/logo_blue.png"
         alt="GamMa banner"
         width="100%"
         style="display:block;border:0;outline:none;text-decoration:none;height:auto;max-height:220px;object-fit:contain;background-color:#ffffff;padding:16px 0;">
  </a>

  <!-- MAIN CONTAINER -->
  <div style="width:90%;max-width:720px;margin:16px auto 24px auto;background-color:#ffffff;
              padding:20px;color:#000000;line-height:1.55;font-size:0.95rem;
              box-shadow:0 6px 22px rgba(0,0,0,0.10);">

    <p style="margin:0 0 14px;"><strong>Kære GamMa-medlem,</strong></p>

    <p style="margin:0 0 18px;">
      Her kommer en kort opdatering med kommende arrangementer fra GamMa.
    </p>

    <!-- START: EVENTS -->

{{EventBlocks}}
{{NewsBlocks}}

<!-- END: EVENTS -->

    <p style="margin:18px 0 0;">
      Vi håber, du har lyst til at kigge forbi og tage en ven med, hvis du kender en GamMa''er, der også bør få det med.
    </p>

    <p style="margin:8px 0 0;">
      Med venlig hilsen<br>
      <strong>GamMas bestyrelse</strong>
    </p>

  </div>

  <div style="height:16px;"></div>
</div>

<div style="height:1px;background-color:#e6e6e6;width:90%;max-width:720px;margin:0 auto 12px auto;"></div>

<!-- Membership -->
<div style="width:90%;max-width:720px;margin:0 auto 24px auto;color:#000000;line-height:1.45;font-size:0.9rem;">
  <div style="padding:16px;background-color:#fffefd;border-radius:10px;border:1px solid #f1f1f1;">
    <p style="margin:0 0 8px;font-weight:bold;">Medlemskab?</p>
    <p style="margin:0 0 8px;">Husk, at deltagelse i GamMa-arrangementer kræver et aktivt medlemskab.</p>
    <p style="margin:0 0 8px;">
      Tjek betaling og opdater dine oplysninger på
      <a href="https://www.gam-ma.dk/Identity/Account/Manage" target="_blank" style="color:#337ab7;text-decoration:none;">
        GamMa.dk → <span style="font-weight:bold;">Profil</span>
      </a>.
    </p>
    <p style="margin:0 0 8px;">Bliv medlem sådan her:</p>
    <ul style="margin:0 0 10px;padding-left:20px;">
      <li>MobilePay: <strong>150 kr. til 22766</strong></li>
      <li>Log ind på <a href="https://gam-ma.dk/" style="color:#337ab7;text-decoration:none;">gam-ma.dk</a> og betal med kort under <em>Menu &gt;&gt; Betal</em></li>
      <li>Skan/tryk på QR-koden nedenfor</li>
    </ul>
    <p style="margin:0 0 10px;text-align:center;">
      <a href="https://mobilepay.dk/erhverv/betalingslink/betalingslink-svar?phone=22766&amp;amount=150&amp;comment=Medlemskab" target="_blank" style="text-decoration:none;">
        <img src="https://lh7-rt.googleusercontent.com/docsz/AD_4nXdzlpmoaRPEsXlhWfS76cmyJHopn0ji3CFV1v86x-DkurRa-LThEnuVntyyz_TKWq4tRvNRxZatoo88bb_rs3aaczNZWpWHxotuJ6quVDo4NXxpXu6ZEJsyEW1Ig5IR0xfB8fM8?key=AprQhRqq-JWjeqGFDCqqvMz9" alt="Medlemskab MobilePay QR" width="150" style="border:0;display:block;margin:0 auto;height:auto;">
      </a>
    </p>
    <p style="margin:0;text-align:center;font-size:0.75rem;">
      Ønsker du at blive afmeldt/udmeldt, så log ind på
      <a href="https://www.gam-ma.dk/Identity/Account/Manage" target="_blank" style="color:#337ab7;text-decoration:none;">GamMa.dk → Privatdata</a>
      og klik <b>''slet''</b>.
    </p>
  </div>
</div>

<div style="height:1px;background-color:#e6e6e6;width:90%;max-width:720px;margin:0 auto 12px auto;"></div>

<!-- Social icons -->
<div style="width:90%;max-width:720px;margin:0 auto 24px auto;text-align:center;background-color:#ffffff;border-radius:10px;">
  <table role="presentation" cellpadding="0" cellspacing="0" style="width:100%;border-collapse:collapse;border:none;background:none;">
    <tbody>
      <tr>
        <td style="padding:10px;border:none;background:none;">
          <a href="https://www.linkedin.com/company/gamma-math-ucph/" target="_blank" style="text-decoration:none;">
            <img src="https://cdn-icons-png.flaticon.com/512/174/174857.png" alt="LinkedIn" width="40" style="display:block;margin:0 auto;border:0;height:auto;">
            <div style="margin-top:6px;font-size:0.7rem;color:#000000;">LinkedIn</div>
          </a>
        </td>
        <td style="padding:10px;border:none;background:none;">
          <a href="https://www.instagram.com/gamma_ku_2100?igsh=MTFjYmk4M213djc0NA==" target="_blank" style="text-decoration:none;">
            <img src="https://cdn-icons-png.flaticon.com/512/2111/2111463.png" alt="Instagram" width="40" style="display:block;margin:0 auto;border:0;height:auto;">
            <div style="margin-top:6px;font-size:0.7rem;color:#000000;">Instagram</div>
          </a>
        </td>
        <td style="padding:10px;border:none;background:none;">
          <a href="https://www.facebook.com/share/g/1Exv7epudc/?mibextid=wwXIfr" target="_blank" style="text-decoration:none;">
            <img src="https://cdn-icons-png.flaticon.com/512/733/733547.png" alt="Facebook" width="40" style="display:block;margin:0 auto;border:0;height:auto;">
            <div style="margin-top:6px;font-size:0.7rem;color:#000000;">Facebook</div>
          </a>
        </td>
      </tr>
    </tbody>
  </table>
</div>

<div style="height:16px;"></div>
',
  'Kommende arrangementer fra GamMa
{{EventBlocks}}
{{NewsBlocks}}',
  'EVENT',
  1,
  '2026-08-20 10:30:00',
  '2026-08-20 10:30:00'
);

INSERT INTO `EmailTemplates` (`Name`, `Subject`, `Preheader`, `HtmlBody`, `TextBody`, `TemplateType`, `IsActive`, `Created`, `Updated`) VALUES
(
  'System - Opret ny bruger',
  'Bekræft din email',
  'Bekræft din GamMa-bruger',
  '<div style="display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;">{{Heading}}</div><div style="margin:0;padding:0;background-color:#eaf2fb;width:100%;font-family:Arial,sans-serif;color:#173042;"><a href="https://gam-ma.dk/" target="_blank" style="text-decoration:none;display:block;"><img src="https://gam-ma.dk/lib/logo_blue.png" alt="GamMa" width="100%" style="display:block;border:0;outline:none;text-decoration:none;height:auto;max-height:180px;object-fit:contain;background-color:#ffffff;padding:16px 0;"></a><div style="width:90%;max-width:640px;margin:18px auto 22px auto;background-color:#ffffff;padding:28px 24px;line-height:1.55;font-size:15px;border:1px solid #d9e7f5;"><p style="margin:0 0 12px;">Hej <strong>{{Name}}</strong></p><h1 style="margin:0 0 14px;font-size:28px;line-height:1.15;color:#10233a;">{{Heading}}</h1><p style="margin:0 0 18px;">{{Intro}}</p><a href="{{ActionUrl}}" target="_blank" style="display:block;width:100%;box-sizing:border-box;text-align:center;background-color:#2485c7;color:#ffffff;text-decoration:none;padding:13px 18px;font-size:15px;font-weight:bold;">{{ActionText}}</a><p style="margin:18px 0 0;color:#4f6475;font-size:14px;">{{Note}}</p><p style="margin:18px 0 0;">Med venlig hilsen<br><strong>GamMas bestyrelse</strong></p></div><div style="height:1px;background-color:#cfe0ef;width:90%;max-width:640px;margin:0 auto 12px auto;"></div><div style="width:90%;max-width:640px;margin:0 auto 24px auto;text-align:center;background-color:#ffffff;"><table role="presentation" cellpadding="0" cellspacing="0" style="width:100%;border-collapse:collapse;border:none;background:none;"><tbody><tr><td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.linkedin.com/company/gamma-math-ucph/" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/174/174857.png" alt="LinkedIn" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">LinkedIn</div></a></td><td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.instagram.com/gamma_ku_2100?igsh=MTFjYmk4M213djc0NA==" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/2111/2111463.png" alt="Instagram" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">Instagram</div></a></td><td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.facebook.com/share/g/1Exv7epudc/?mibextid=wwXIfr" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/733/733547.png" alt="Facebook" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">Facebook</div></a></td></tr></tbody></table></div><div style="height:16px;"></div></div>',
  'Hej {{Name}}

{{Intro}}

{{ActionText}}: {{ActionUrl}}

{{Note}}

Med venlig hilsen
GamMas bestyrelse',
  'SYSTEM_REGISTRATION_CONFIRMATION',
  1,
  '2026-09-03 12:00:00',
  '2026-09-03 12:00:00'
),
(
  'System - Bekræft email',
  'Bekræft din email',
  'Bekræft din email hos GamMa',
  '<div style="display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;">{{Heading}}</div><div style="margin:0;padding:0;background-color:#eaf2fb;width:100%;font-family:Arial,sans-serif;color:#173042;"><a href="https://gam-ma.dk/" target="_blank" style="text-decoration:none;display:block;"><img src="https://gam-ma.dk/lib/logo_blue.png" alt="GamMa" width="100%" style="display:block;border:0;outline:none;text-decoration:none;height:auto;max-height:180px;object-fit:contain;background-color:#ffffff;padding:16px 0;"></a><div style="width:90%;max-width:640px;margin:18px auto 22px auto;background-color:#ffffff;padding:28px 24px;line-height:1.55;font-size:15px;border:1px solid #d9e7f5;"><p style="margin:0 0 12px;">Hej <strong>{{Name}}</strong></p><h1 style="margin:0 0 14px;font-size:28px;line-height:1.15;color:#10233a;">{{Heading}}</h1><p style="margin:0 0 18px;">{{Intro}}</p><a href="{{ActionUrl}}" target="_blank" style="display:block;width:100%;box-sizing:border-box;text-align:center;background-color:#2485c7;color:#ffffff;text-decoration:none;padding:13px 18px;font-size:15px;font-weight:bold;">{{ActionText}}</a><p style="margin:18px 0 0;color:#4f6475;font-size:14px;">{{Note}}</p><p style="margin:18px 0 0;">Med venlig hilsen<br><strong>GamMas bestyrelse</strong></p></div><div style="height:1px;background-color:#cfe0ef;width:90%;max-width:640px;margin:0 auto 12px auto;"></div><div style="width:90%;max-width:640px;margin:0 auto 24px auto;text-align:center;background-color:#ffffff;"><table role="presentation" cellpadding="0" cellspacing="0" style="width:100%;border-collapse:collapse;border:none;background:none;"><tbody><tr><td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.linkedin.com/company/gamma-math-ucph/" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/174/174857.png" alt="LinkedIn" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">LinkedIn</div></a></td><td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.instagram.com/gamma_ku_2100?igsh=MTFjYmk4M213djc0NA==" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/2111/2111463.png" alt="Instagram" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">Instagram</div></a></td><td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.facebook.com/share/g/1Exv7epudc/?mibextid=wwXIfr" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/733/733547.png" alt="Facebook" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">Facebook</div></a></td></tr></tbody></table></div><div style="height:16px;"></div></div>',
  'Hej {{Name}}

{{Intro}}

{{ActionText}}: {{ActionUrl}}

{{Note}}

Med venlig hilsen
GamMas bestyrelse',
  'SYSTEM_EMAIL_CONFIRMATION',
  1,
  '2026-09-03 12:00:00',
  '2026-09-03 12:00:00'
),
(
  'System - Ændr email',
  'Bekræft email',
  'Bekræft din nye email hos GamMa',
  '<div style="display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;">{{Heading}}</div><div style="margin:0;padding:0;background-color:#eaf2fb;width:100%;font-family:Arial,sans-serif;color:#173042;"><a href="https://gam-ma.dk/" target="_blank" style="text-decoration:none;display:block;"><img src="https://gam-ma.dk/lib/logo_blue.png" alt="GamMa" width="100%" style="display:block;border:0;outline:none;text-decoration:none;height:auto;max-height:180px;object-fit:contain;background-color:#ffffff;padding:16px 0;"></a><div style="width:90%;max-width:640px;margin:18px auto 22px auto;background-color:#ffffff;padding:28px 24px;line-height:1.55;font-size:15px;border:1px solid #d9e7f5;"><p style="margin:0 0 12px;">Hej <strong>{{Name}}</strong></p><h1 style="margin:0 0 14px;font-size:28px;line-height:1.15;color:#10233a;">{{Heading}}</h1><p style="margin:0 0 18px;">{{Intro}}</p><a href="{{ActionUrl}}" target="_blank" style="display:block;width:100%;box-sizing:border-box;text-align:center;background-color:#2485c7;color:#ffffff;text-decoration:none;padding:13px 18px;font-size:15px;font-weight:bold;">{{ActionText}}</a><p style="margin:18px 0 0;color:#4f6475;font-size:14px;">{{Note}}</p><p style="margin:18px 0 0;">Med venlig hilsen<br><strong>GamMas bestyrelse</strong></p></div><div style="height:1px;background-color:#cfe0ef;width:90%;max-width:640px;margin:0 auto 12px auto;"></div><div style="width:90%;max-width:640px;margin:0 auto 24px auto;text-align:center;background-color:#ffffff;"><table role="presentation" cellpadding="0" cellspacing="0" style="width:100%;border-collapse:collapse;border:none;background:none;"><tbody><tr><td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.linkedin.com/company/gamma-math-ucph/" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/174/174857.png" alt="LinkedIn" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">LinkedIn</div></a></td><td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.instagram.com/gamma_ku_2100?igsh=MTFjYmk4M213djc0NA==" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/2111/2111463.png" alt="Instagram" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">Instagram</div></a></td><td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.facebook.com/share/g/1Exv7epudc/?mibextid=wwXIfr" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/733/733547.png" alt="Facebook" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">Facebook</div></a></td></tr></tbody></table></div><div style="height:16px;"></div></div>',
  'Hej {{Name}}

{{Intro}}

{{ActionText}}: {{ActionUrl}}

{{Note}}

Med venlig hilsen
GamMas bestyrelse',
  'SYSTEM_EMAIL_CHANGE_CONFIRMATION',
  1,
  '2026-09-03 12:00:00',
  '2026-09-03 12:00:00'
),
(
  'System - Nulstil password',
  'Nulstil Password',
  'Nulstil dit password hos GamMa',
  '<div style="display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;">{{Heading}}</div><div style="margin:0;padding:0;background-color:#eaf2fb;width:100%;font-family:Arial,sans-serif;color:#173042;"><a href="https://gam-ma.dk/" target="_blank" style="text-decoration:none;display:block;"><img src="https://gam-ma.dk/lib/logo_blue.png" alt="GamMa" width="100%" style="display:block;border:0;outline:none;text-decoration:none;height:auto;max-height:180px;object-fit:contain;background-color:#ffffff;padding:16px 0;"></a><div style="width:90%;max-width:640px;margin:18px auto 22px auto;background-color:#ffffff;padding:28px 24px;line-height:1.55;font-size:15px;border:1px solid #d9e7f5;"><p style="margin:0 0 12px;">Hej <strong>{{Name}}</strong></p><h1 style="margin:0 0 14px;font-size:28px;line-height:1.15;color:#10233a;">{{Heading}}</h1><p style="margin:0 0 18px;">{{Intro}}</p><a href="{{ActionUrl}}" target="_blank" style="display:block;width:100%;box-sizing:border-box;text-align:center;background-color:#2485c7;color:#ffffff;text-decoration:none;padding:13px 18px;font-size:15px;font-weight:bold;">{{ActionText}}</a><p style="margin:18px 0 0;color:#4f6475;font-size:14px;">{{Note}}</p><p style="margin:18px 0 0;">Med venlig hilsen<br><strong>GamMas bestyrelse</strong></p></div><div style="height:1px;background-color:#cfe0ef;width:90%;max-width:640px;margin:0 auto 12px auto;"></div><div style="width:90%;max-width:640px;margin:0 auto 24px auto;text-align:center;background-color:#ffffff;"><table role="presentation" cellpadding="0" cellspacing="0" style="width:100%;border-collapse:collapse;border:none;background:none;"><tbody><tr><td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.linkedin.com/company/gamma-math-ucph/" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/174/174857.png" alt="LinkedIn" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">LinkedIn</div></a></td><td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.instagram.com/gamma_ku_2100?igsh=MTFjYmk4M213djc0NA==" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/2111/2111463.png" alt="Instagram" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">Instagram</div></a></td><td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.facebook.com/share/g/1Exv7epudc/?mibextid=wwXIfr" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/733/733547.png" alt="Facebook" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">Facebook</div></a></td></tr></tbody></table></div><div style="height:16px;"></div></div>',
  'Hej {{Name}}

{{Intro}}

{{ActionText}}: {{ActionUrl}}

{{Note}}

Med venlig hilsen
GamMas bestyrelse',
  'SYSTEM_PASSWORD_RESET',
  1,
  '2026-09-03 12:00:00',
  '2026-09-03 12:00:00'
);

SET FOREIGN_KEY_CHECKS=1;
