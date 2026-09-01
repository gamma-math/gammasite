SET NAMES utf8mb4;

DELETE FROM `EventRegistrations`;
DELETE FROM `ContentLinks`;
DELETE FROM `ContentItems`;

INSERT INTO `ContentItems` (
  `Title`, `Slug`, `Summary`, `Body`, `PictureUrl`, `Tags`, `Type`, `Status`,
  `StartDate`, `EndDate`, `Location`, `CreatedByUserId`, `Created`, `Updated`, `PublishedAt`
) VALUES
(
  'Karriere efter matematik',
  'karriere-efter-matematik',
  'Mød alumner fra analyse, data science og undervisning til en aften om veje efter studiet.',
  'Vi samler en lille paneldebat med tidligere matematikere, som fortæller ærligt om deres første job, skift undervejs og hvad de ville ønske, de havde vidst som studerende.',
  'https://images.unsplash.com/photo-1517245386807-bb43f82c33c4?auto=format&fit=crop&w=1200&q=80',
  'event,karriere,alumner',
  'EVENT',
  'PUBLISHED',
  '2026-09-25 17:00:00',
  '2026-09-25 20:00:00',
  'HCØ, Universitetsparken 5, 2100 København Ø',
  (SELECT `Id` FROM `AspNetUsers` WHERE `Email` = 'admin.test@gamma.local' LIMIT 1),
  '2026-08-20 10:00:00',
  '2026-08-20 10:00:00',
  '2026-08-20 10:00:00'
),
(
  'GamMa julebanko',
  'gamma-julebanko-2026',
  'Årets hyggeligste bankoaften med æbleskiver, præmier og godt selskab.',
  'Tag en medstuderende eller gammel læsemakker under armen. Vi sørger for bankoplader, kaffe og et præmiebord med passende matematisk absurditet.',
  'https://images.unsplash.com/photo-1512389142860-9c449e58a543?auto=format&fit=crop&w=1200&q=80',
  'event,socialt,jul',
  'EVENT',
  'PUBLISHED',
  '2026-12-03 18:30:00',
  '2026-12-03 22:00:00',
  'BioCaféen, Universitetsparken',
  (SELECT `Id` FROM `AspNetUsers` WHERE `Email` = 'admin.test@gamma.local' LIMIT 1),
  '2026-08-22 12:00:00',
  '2026-08-22 12:00:00',
  '2026-08-22 12:00:00'
),
(
  'Sommermøde i Fælledparken',
  'sommermoede-i-faelledparken',
  'Et uformelt sommermøde for medlemmer, alumner og studerende.',
  'Vi mødes til let mad, netværk og korte updates fra bestyrelsen. Arrangementet er åbent for alle medlemmer.',
  'https://images.unsplash.com/photo-1475721027785-f74eccf877e2?auto=format&fit=crop&w=1200&q=80',
  'event,sommer,netværk',
  'EVENT',
  'DRAFT',
  '2027-06-12 16:00:00',
  '2027-06-12 19:00:00',
  'Fælledparken',
  (SELECT `Id` FROM `AspNetUsers` WHERE `Email` = 'admin.test@gamma.local' LIMIT 1),
  '2026-08-24 09:30:00',
  '2026-08-24 09:30:00',
  NULL
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
  (SELECT `Id` FROM `AspNetUsers` WHERE `Email` = 'admin.test@gamma.local' LIMIT 1),
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
  (SELECT `Id` FROM `AspNetUsers` WHERE `Email` = 'admin.test@gamma.local' LIMIT 1),
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
  (SELECT `Id` FROM `AspNetUsers` WHERE `Email` = 'admin.test@gamma.local' LIMIT 1),
  '2026-08-28 16:10:00',
  '2026-08-28 16:10:00',
  NULL
);
