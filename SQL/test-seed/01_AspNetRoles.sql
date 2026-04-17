SET NAMES utf8mb4;

DELETE FROM `AspNetRoles`;

INSERT INTO `AspNetRoles` (`Id`, `Name`, `NormalizedName`, `ConcurrencyStamp`) VALUES
('role-admin', 'Admin', 'ADMIN', '3bff48cf-bde8-43f2-8e4f-0d27d5f9ec01'),
('role-test', 'Test', 'TEST', '73f673e8-b29b-46dc-9bf1-8ca4a9422cc1'),
('role-finance', 'Finance', 'FINANCE', '793dddb1-a177-4a9a-9ff7-a2224f8533e1'),
('role-mail', 'Rolle mail test', 'ROLLE MAIL TEST', '2c2212b9-5c03-4f83-94fd-97769ad33dc4');
