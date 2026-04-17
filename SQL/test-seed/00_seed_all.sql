-- Test seed for GAMMASITE
-- Uses fake test data only.

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS=0;

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
('user-fake-008', '88888888-8888-4888-8888-888888888888', 'oliver.test@gamma.local', 1, 'AQAAAAIAAYagAAAAEP79bP/4vOktDMCCwxmL7/X8IbdIJ0xdeXjVsHyDb345QvrdYZT/89u8OtAYzWGTag==', '88888888-aaaa-bbbb-cccc-888888888888', '20110008', 0, 0, NULL, 1, 0, 'oliver.test@gamma.local', NULL, 'OLIVER.TEST@GAMMA.LOCAL', 'OLIVER.TEST@GAMMA.LOCAL', 'Oliver Testsen', 'Eksempelvej 17', 0, 1, 2025, 'Studerende', '0001-01-01 00:00:00', '2024-06-01 10:10:00');

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

SET FOREIGN_KEY_CHECKS=1;
