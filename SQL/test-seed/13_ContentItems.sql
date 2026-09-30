SET NAMES utf8mb4;

DELETE FROM `EventRegistrations`;
DELETE FROM `ContentLinks`;
DELETE FROM `ContentItems`;

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
  (SELECT `Id` FROM `AspNetUsers` WHERE `Email` = 'admin.test@gamma.local' LIMIT 1),
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
  (SELECT `Id` FROM `AspNetUsers` WHERE `Email` = 'admin.test@gamma.local' LIMIT 1),
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
  (SELECT `Id` FROM `AspNetUsers` WHERE `Email` = 'admin.test@gamma.local' LIMIT 1),
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
  (SELECT `Id` FROM `AspNetUsers` WHERE `Email` = 'admin.test@gamma.local' LIMIT 1),
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
  (SELECT `Id` FROM `AspNetUsers` WHERE `Email` = 'admin.test@gamma.local' LIMIT 1),
  '2026-09-01 12:00:00',
  '2026-09-01 12:00:00',
  '2026-09-01 12:00:00'
),
(
  'Faglig aften - 7. oktober',
  'faglig-aften-oktober-2026',
  'To færdiguddannede matematikere fortæller om software, C*-algebraer og database performance tuning.',
  CONCAT(
    '<h2>Faglig aften d. 7. oktober</h2>',
    '<p>Onsdag den <strong>7. oktober 2026</strong> er det blevet tid til at holde en ny faglig aften, hvor to færdiguddannede matematikere vil komme og fortælle lidt om et emne, de brænder for.</p>',
    '<p>Denne gang er scenen sat for Morten Ankerstjerne (aka Agent A) og Matias Lolk Andersen. Se nedenfor for information om oplæggene.</p>',
    '<p>Som sædvanligt vil GamMa stå for lidt mad og drikke til arrangementet, som derfor kræver tilmelding. Dette kan ske via Facebook-eventet eller på mail. Vi glæder os til at se jer.</p>',
    '<p><strong>Universitetsparken 5 kl. 17:30</strong></p>',
    '<h3>Matias oplæg: Software og C*-algebraer</h3>',
    '<p>Det er et fundamentalt ubesvaret spørgsmål i ikke-kommutativ ringteori, om der findes en ikke-separativ exchange ring. For C*-algebraer er dette nært beslægtet til et spørgsmål af Rørdam om, hvorvidt der findes en simpel, reel rang nul C*-algebra indeholdende både en ikke-triviel endelig og en uendelig projektion.</p>',
    '<p>I min PhD-afhandling viste jeg blandt andet, at de såkaldt separerede graf-C*-algebraer ikke i sig selv giver anledning til et sådant eksempel - i stedet må de nærmere betragtes som plausible byggesten, der måske kan approksimere det eftertragtede eksempel. I et samarbejde med Pere Ara fandt vi også frem til en naturlig strategi for en sådan approksimation, men i praksis var vi ikke i stand til at forfølge den på grund af de uoverkommeligt mange beregninger, der skulle til, for bare at identificere de første trin.</p>',
    '<p>Her ni år efter mit exit fra Akademia er problemet imidlertid igen begyndt at røre på sig i mit baghoved, og med erfaring fra software-branchen burde det ikke være et større problem at implementere de nødvendige algoritmer. Jeg lover at tegne en masse grafer og sige mindst muligt om ikke-kommutativ algebra! Og hvis jeg finder tilstrækkelig med tid inden til at implementere de relevante algoritmer, vil jeg fortælle om de indledende resultater.</p>',
    '<h3>Mortens oplæg: Introduktion til database performance tuning - Hvordan finder SQL Server frem til data?</h3>',
    '<p>Vi har nok alle været i kontakt med databaser på ét eller andet tidspunkt i vores liv. For de fleste af os er det heldigvis et ret abstrakt begreb, som den software vi bruger, sørger for at håndtere for os. Mange af os har også fornøjelsen af at skulle udvikle software - software som før eller siden skal gemme information til senere brug, så den kan findes frem igen.</p>',
    '<p>Men hvordan fungerer det lige i praksis? Og hvad kan man gøre, når databasen pludselig begynder at bruge lang tid på at finde den information man beder om? Jeg vil tage udgangspunkt i Microsoft SQL Server, der er et såkaldt relationelt databasesystem, og forsøge at give en lavpraktisk intuition for, hvordan data bliver opbevaret, opdateret og hentet, og hvordan de rette indexes kan gøre SQL Servers arbejde lettere - og måske også hvordan de forkerte indexes kan gøre det sværere.</p>'
  ),
  'https://images.unsplash.com/photo-1532094349884-543bc11b234d?auto=format&fit=crop&w=1200&q=80',
  'event,faglig-aften,matematik,software,database',
  'EVENT',
  'PUBLISHED',
  '2026-10-07 17:30:00',
  '2026-10-07 19:30:00',
  'Universitetsparken 5',
  (SELECT `Id` FROM `AspNetUsers` WHERE `Email` = 'admin.test@gamma.local' LIMIT 1),
  '2026-09-08 10:00:00',
  '2026-09-08 10:00:00',
  '2026-09-08 10:00:00'
);

INSERT INTO `ContentItems` (
  `Title`, `Slug`, `Summary`, `Body`, `PictureUrl`, `Tags`, `Type`, `Status`,
  `StartDate`, `EndDate`, `Location`, `CreatedByUserId`, `Created`, `Updated`, `PublishedAt`
) VALUES (
  'Ny hjemmeside',
  'ny-hjemmeside-2026',
  'GamMa har fået en ny hjemmeside med bedre overblik over arrangementer, nyheder og fællesskab.',
  CONCAT(
    '<h2>Ny hjemmeside - mere liv, flere arrangementer og bedre fællesskab</h2>',
    '<p>GamMa har fået en ny hjemmeside!</p>',
    '<p>Den nye hjemmeside gør det nemmere at følge med i, hvad der sker i foreningen, og giver os bedre muligheder for at skabe mere aktivitet omkring GamMa.</p>',
    '<p>På hjemmesiden kan du blandt andet:</p>',
    '<ul>',
    '<li>Se kommende arrangementer og tilmelde dig direkte.</li>',
    '<li>Læse nyheder fra foreningen.</li>',
    '<li>Få et bedre overblik over faglige og sociale aktiviteter.</li>',
    '<li>Følge med i tidligere og kommende initiativer.</li>',
    '<li>Finde information om GamMa og vores fællesskab.</li>',
    '</ul>',
    '<p>Vi arbejder løbende på at udbygge hjemmesiden, så den bliver det naturlige sted at finde information om arrangementer, nyheder og aktiviteter i foreningen.</p>',
    '<p>Tag et kig rundt på den nye hjemmeside, og husk at holde øje med både nyheder og arrangementer.</p>',
    '<p>Vi glæder os til at se dig online - og selvfølgelig til GamMa''s kommende arrangementer!</p>'
  ),
  'https://github.com/AbrahimBorgiPrivat/GAMMA---ASSETS/blob/main/res/img/nyheder/new_homepage.png?raw=true',
  'nyhed,hjemmeside',
  'NEWS',
  'PUBLISHED',
  NULL,
  NULL,
  NULL,
  (SELECT `Id` FROM `AspNetUsers` WHERE `Email` = 'admin.test@gamma.local' LIMIT 1),
  '2026-09-08 10:00:00',
  '2026-09-08 10:00:00',
  '2026-09-08 10:00:00'
);
