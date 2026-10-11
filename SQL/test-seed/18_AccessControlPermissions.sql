-- Test seed for permissions, role-permissions and `AspNetRoleClaims`.
-- Existing access-control test data is reset before it is seeded again.

SET NAMES utf8mb4;

DELETE FROM `AspNetRoleClaims`;
DELETE FROM `RolePermissions`;
DELETE FROM `Permissions`;

INSERT INTO `Permissions` (`Code`, `Description`) VALUES
  ('content.edit', 'Se, oprette, redigere, publicere og slette events og nyheder'),
  ('registrations.edit', 'Redigere tilmeldinger til alle events'),
  ('finance.view.all', 'Se alle finanser'),
  ('finance.edit.all', 'Se og redigere alle finanser'),
  ('messages.edit', CONVERT(0x5365206265736B65646F6D72C3A5646574206F672073656E6465206265736B65646572 USING utf8mb4)),
  ('email_templates.edit', 'Se, oprette, redigere og slette beskedskabeloner'),
  ('roles.edit', CONVERT(0x5365206F672061646D696E697374726572652068656C6520726F6C6C652D206F67207065726D697373696F6E736F6D72C3A5646574 USING utf8mb4))
ON DUPLICATE KEY UPDATE
  `Description` = VALUES(`Description`);

-- Admin receives every defined permission.
INSERT IGNORE INTO `RolePermissions` (`RoleId`, `PermissionId`)
SELECT 'role-admin', `Id`
FROM `Permissions`
WHERE `Code` IN (
  'content.edit',
  'registrations.edit',
  'finance.view.all',
  'finance.edit.all',
  'messages.edit',
  'email_templates.edit',
  'roles.edit'
);

INSERT IGNORE INTO `RolePermissions` (`RoleId`, `PermissionId`)
SELECT 'role-kasserer', `Id`
FROM `Permissions`
WHERE `Code` IN ('finance.view.all', 'finance.edit.all');

INSERT IGNORE INTO `RolePermissions` (`RoleId`, `PermissionId`)
SELECT 'role-revisor', `Id`
FROM `Permissions`
WHERE `Code` = 'finance.view.all';

INSERT IGNORE INTO `RolePermissions` (`RoleId`, `PermissionId`)
SELECT 'role-arrangoer', `Id`
FROM `Permissions`
WHERE `Code` IN ('content.edit', 'registrations.edit');

INSERT IGNORE INTO `RolePermissions` (`RoleId`, `PermissionId`)
SELECT 'role-kommunikation', `Id`
FROM `Permissions`
WHERE `Code` IN ('messages.edit', 'email_templates.edit');

-- Keep the same permissions available as ASP.NET Identity role claims.
INSERT INTO `AspNetRoleClaims` (`ClaimType`, `ClaimValue`, `RoleId`)
SELECT 'permission', p.`Code`, rp.`RoleId`
FROM `RolePermissions` rp
INNER JOIN `Permissions` p ON p.`Id` = rp.`PermissionId`
WHERE NOT EXISTS (
  SELECT 1
  FROM `AspNetRoleClaims` existing
  WHERE existing.`ClaimType` = 'permission'
    AND existing.`ClaimValue` = p.`Code`
    AND existing.`RoleId` = rp.`RoleId`
);
