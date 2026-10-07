SET NAMES utf8mb4;

DELETE FROM `AspNetRoles`;

INSERT INTO `AspNetRoles` (`Id`, `Name`, `NormalizedName`, `ConcurrencyStamp`) VALUES
('role-admin', 'Admin', 'ADMIN', '3bff48cf-bde8-43f2-8e4f-0d27d5f9ec01'),
('role-test', 'Test', 'TEST', '73f673e8-b29b-46dc-9bf1-8ca4a9422cc1'),
('role-finance', 'Finance', 'FINANCE', '793dddb1-a177-4a9a-9ff7-a2224f8533e1'),
('role-mail', 'Rolle mail test', 'ROLLE MAIL TEST', '2c2212b9-5c03-4f83-94fd-97769ad33dc4'),
('role-kasserer', 'Kasserer', 'KASSERER', 'd7b5c54e-1a7a-4d42-a7eb-7e12f45c3f01'),
('role-revisor', 'Revisor', 'REVISOR', 'e0c7bd31-8935-4db8-88b8-72b0c5b2a302'),
('role-arrangoer', CONVERT(0x417272616E67C3B872 USING utf8mb4), CONVERT(0x415252414E47C39852 USING utf8mb4), 'f1a6df40-9e4d-4fa7-8e6f-6e5d7f8d7c03'),
('role-kommunikation', 'Kommunikationsansvarlig', 'KOMMUNIKATIONSANSVARLIG', 'a8e5a4f9-2f06-4a7b-ae11-2d1eb4c7d504')
ON DUPLICATE KEY UPDATE
  `Name` = VALUES(`Name`),
  `NormalizedName` = VALUES(`NormalizedName`),
  `ConcurrencyStamp` = VALUES(`ConcurrencyStamp`);
