SET NAMES utf8mb4;

DELETE FROM `EmailTemplates`;

INSERT INTO `EmailTemplates` (`Name`, `Subject`, `Preheader`, `HtmlBody`, `TextBody`, `TemplateType`, `IsActive`, `Created`, `Updated`) VALUES
(
  'Eventbekræftelse',
  'Du er tilmeldt {{EventTitle}}',
  'Vi glæder os til at se dig.',
  '<h1>Hej {{UserName}}</h1><p>Du er nu tilmeldt <strong>{{EventTitle}}</strong>.</p><p>Tidspunkt: {{EventStartDate}}</p><p><a href="{{EventRegisterUrl}}">Se eventet</a></p>',
  'Hej {{UserName}}\nDu er nu tilmeldt {{EventTitle}}.\nTidspunkt: {{EventStartDate}}\n{{EventRegisterUrl}}',
  'EVENT_CONFIRMATION',
  1,
  '2026-08-20 10:30:00',
  '2026-08-20 10:30:00'
),
(
  'Eventpåmindelse',
  'Påmindelse: {{EventTitle}}',
  'Dit GamMa-event starter snart.',
  '<h1>{{EventTitle}}</h1><p>Hej {{UserName}}, dette er en venlig påmindelse om arrangementet.</p><p>{{EventStartDate}}</p>',
  'Påmindelse: {{EventTitle}}\nHej {{UserName}}, arrangementet starter {{EventStartDate}}.',
  'EVENT_REMINDER',
  1,
  '2026-08-20 10:35:00',
  '2026-08-20 10:35:00'
),
(
  'Nyhedsbrev basis',
  'Nyt fra GamMa',
  'Seneste nyt, events og medlemsinfo.',
  '<h1>Nyt fra GamMa</h1><div>{{ContentBlocks}}</div><p>Du kan opdatere dine oplysninger her: <a href="{{ProfileUrl}}">Profil</a></p>',
  'Nyt fra GamMa\n{{ContentBlocks}}\nProfil: {{ProfileUrl}}',
  'NEWSLETTER',
  1,
  '2026-08-26 09:00:00',
  '2026-08-26 09:00:00'
);
