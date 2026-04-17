-- Test seed for `AspNetUserTokens`
-- Uses fake token values only.

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS=0;
DELETE FROM `AspNetUserTokens`;
SET FOREIGN_KEY_CHECKS=1;

INSERT INTO `AspNetUserTokens` (`UserId`, `LoginProvider`, `Name`, `Value`) VALUES
('user-admin-test', '[AspNetUserStore]', 'AuthenticatorKey', 'TESTADMINAUTHKEY0000000000000001'),
('user-member-test', '[AspNetUserStore]', 'AuthenticatorKey', 'TESTMEMBERAUTHKEY000000000000001'),
('user-fake-003', '[AspNetUserStore]', 'AuthenticatorKey', 'TESTUSERAUTHKEY000000000000000003'),
('user-fake-005', '[AspNetUserStore]', 'RecoveryCodes', 'code001;code002;code003;code004;code005');
