SET NAMES utf8mb4;

DELETE FROM `AspNetUserRoles`;

INSERT INTO `AspNetUserRoles` (`UserId`, `RoleId`) VALUES
('user-admin-test', 'role-admin'),
('user-admin-test', 'role-finance'),
('user-member-test', 'role-test'),
('user-fake-001', 'role-admin'),
('user-fake-002', 'role-test'),
('user-fake-003', 'role-mail'),
('user-fake-005', 'role-finance');
