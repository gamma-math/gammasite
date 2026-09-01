SET NAMES utf8mb4;

DELETE FROM `ContentLinks`;

INSERT INTO `ContentLinks` (`ContentItemId`, `Label`, `Url`, `Type`, `SortOrder`, `Created`, `Updated`) VALUES
((SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'karriere-efter-matematik' LIMIT 1), 'LinkedIn event', 'https://www.linkedin.com/company/gamma-math-ucph/', 'LINKEDIN', 0, '2026-08-20 10:05:00', '2026-08-20 10:05:00'),
((SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'karriere-efter-matematik' LIMIT 1), 'Find vej', 'https://google.com/maps?q=Universitetsparken%205%202100%20K%C3%B8benhavn', 'OTHER', 1, '2026-08-20 10:05:00', '2026-08-20 10:05:00'),
((SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'gamma-julebanko-2026' LIMIT 1), 'Facebook-gruppen', 'https://www.facebook.com/share/g/1Exv7epudc/', 'FACEBOOK', 0, '2026-08-22 12:05:00', '2026-08-22 12:05:00'),
((SELECT `Id` FROM `ContentItems` WHERE `Slug` = 'react-migrationen-er-i-gang' LIMIT 1), 'Læs projektstatus', 'https://github.com/gamma-math', 'OTHER', 0, '2026-08-26 08:50:00', '2026-08-26 08:50:00');
