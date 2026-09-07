SET NAMES utf8mb4;

DELETE FROM `EmailTemplates`;

INSERT INTO `EmailTemplates` (`Name`, `Subject`, `Preheader`, `HtmlBody`, `TextBody`, `TemplateType`, `IsActive`, `Created`, `Updated`) VALUES
(
  'GamMa Summermail Template',
  'Sommernyt fra GamMa',
  'Sommernyt, kommende arrangementer og nyheder fra GamMa',
  '<!-- GammaEmailBlockDesign eventColor="#18a999" newsColor="#1877F2" -->
<!--
  GamMa summer mail template
  Rediger primært indholdet mellem:
  START: CONTENT
  END: CONTENT

  Arbejdsgang:
  1. Opdater preheader og intro.
  2. Tilpas eller kopiér indholdsblokkene efter behov.
  3. Udskift links, arrangører og eventuelle billeder.
-->

<!-- Preheader (hidden in most clients) -->
<div style="display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;">
  [Kort preheader til sommermailen]
</div>

<!-- Outer wrapper -->
<div style="margin:0;padding:0;background-color:#dff5f2;width:100%;font-family:Arial,sans-serif;">

  <!-- Banner -->
  <a href="https://gam-ma.dk/" target="_blank" style="text-decoration:none;display:block;">
    <img src="https://github.com/AbrahimBorgiPrivat/GAMMA---ASSETS/blob/main/res/mails/img/headers/GamMa_Sommer_Bjaelke.jpg?raw=true"
         alt="GamMa sommer banner"
         width="100%"
         style="display:block;border:0;outline:none;text-decoration:none;height:auto;">
  </a>

  <!-- MAIN CONTAINER -->
  <div style="width:90%;max-width:720px;margin:16px auto 24px auto;background-color:#ffffff;
              padding:20px;color:#173042;line-height:1.55;font-size:0.95rem;
              box-shadow:0 6px 22px rgba(20,70,90,0.12);">

    <div style="text-align:center;font-size:18px;letter-spacing:4px;color:#78b7b0;margin:0 0 10px;">SUMMER VIBES</div>

    <p style="margin:0 0 12px;"><strong>K&aelig;re GamMa-medlem,</strong></p>

    <p style="margin:0 0 18px;">
      [Kort sommerlig intro til mailen. Skriv her, hvad mailen handler om, og hvorfor man skal l&aelig;se med.]
    </p>

    <!-- START: CONTENT -->

{{ContentBlocks}}

<!-- END: CONTENT -->

    <p style="margin:18px 0 0;">
      Vi h&aring;ber, du har lyst til at kigge forbi og tage en ven med, hvis du kender en GamMa''er, der ogs&aring; b&oslash;r f&aring; det med.
    </p>

    <p style="margin:8px 0 0;">
      Med venlig hilsen<br>
      <strong>GamMas bestyrelse</strong>
    </p>

    <div style="text-align:center;font-size:18px;letter-spacing:4px;color:#78b7b0;margin:10px 0 0;">SUN AND MATH</div>
  </div>

  <div style="height:1px;background-color:#cfe6e2;width:90%;max-width:720px;margin:0 auto 12px auto;"></div>

  <!-- Membership -->
  <div style="width:90%;max-width:720px;margin:0 auto 24px auto;color:#173042;line-height:1.45;font-size:0.9rem;">
    <div style="padding:16px;background-color:#fffefb;border-radius:10px;border:1px solid #e9efe8;">
      <p style="margin:0 0 8px;font-weight:bold;">Medlemskab?</p>
      <p style="margin:0 0 8px;">Husk, at deltagelse i GamMa-arrangementer kr&aelig;ver et aktivt medlemskab.</p>
      <p style="margin:0 0 8px;">
        Tjek betaling og opdater dine oplysninger p&aring;
        <a href="https://www.gam-ma.dk/Identity/Account/Manage" target="_blank" style="color:#337ab7;text-decoration:none;">
          GamMa.dk - <span style="font-weight:bold;">Profil</span>
        </a>.
      </p>
      <p style="margin:0 0 8px;">Bliv medlem s&aring;dan her:</p>
      <ul style="margin:0 0 10px;padding-left:20px;">
        <li>MobilePay: <strong>150 kr. til 22766</strong></li>
        <li>Log ind p&aring; <a href="https://gam-ma.dk/" style="color:#337ab7;text-decoration:none;">gam-ma.dk</a> og betal med kort under <em>Menu &gt;&gt; Betal</em></li>
        <li>Skan eller tryk p&aring; QR-koden nedenfor</li>
      </ul>
      <p style="margin:0 0 10px;font-size:0.85rem;color:#5b7383;">
        Er du studerende, er medlemskabet gratis. Kontakt bestyrelsen, s&aring; vi kan notere det.
      </p>
      <p style="margin:0 0 10px;text-align:center;">
        <a href="https://mobilepay.dk/erhverv/betalingslink/betalingslink-svar?phone=22766&amp;amount=150&amp;comment=Medlemskab" target="_blank" style="text-decoration:none;">
          <img src="https://github.com/AbrahimBorgiPrivat/GAMMA---ASSETS/blob/main/res/mails/img/qr/QR_medlemskab.png?raw=true"
               alt="Medlemskab MobilePay QR"
               width="150"
               style="border:0;display:block;margin:0 auto;height:auto;">
        </a>
      </p>
      <p style="margin:0;text-align:center;font-size:0.75rem;">
        &Oslash;nsker du at blive afmeldt eller udmeldt, s&aring; log ind p&aring;
        <a href="https://www.gam-ma.dk/Identity/Account/Manage" target="_blank" style="color:#337ab7;text-decoration:none;">GamMa.dk - Privatdata</a>
        og klik <b>''slet''</b>.
      </p>
    </div>
  </div>

  <div style="height:1px;background-color:#cfe6e2;width:90%;max-width:720px;margin:0 auto 12px auto;"></div>

  <!-- Social icons -->
  <div style="width:90%;max-width:720px;margin:0 auto 24px auto;text-align:center;background-color:#ffffff;border-radius:10px;">
    <table role="presentation" cellpadding="0" cellspacing="0" style="width:100%;border-collapse:collapse;border:none;background:none;">
      <tbody>
        <tr>
          <td style="padding:10px;border:none;background:none;text-align:center;">
            <a href="https://www.linkedin.com/company/gamma-math-ucph/" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;">
              <img src="https://cdn-icons-png.flaticon.com/512/174/174857.png" alt="LinkedIn" width="40" style="display:block;margin:0 auto;border:0;height:auto;">
              <div style="margin-top:6px;font-size:0.7rem;color:#000000;text-align:center;">LinkedIn</div>
            </a>
          </td>
          <td style="padding:10px;border:none;background:none;text-align:center;">
            <a href="https://www.instagram.com/gamma_ku_2100?igsh=MTFjYmk4M213djc0NA==" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;">
              <img src="https://cdn-icons-png.flaticon.com/512/2111/2111463.png" alt="Instagram" width="40" style="display:block;margin:0 auto;border:0;height:auto;">
              <div style="margin-top:6px;font-size:0.7rem;color:#000000;text-align:center;">Instagram</div>
            </a>
          </td>
          <td style="padding:10px;border:none;background:none;text-align:center;">
            <a href="https://www.facebook.com/share/g/1Exv7epudc/?mibextid=wwXIfr" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;">
              <img src="https://cdn-icons-png.flaticon.com/512/733/733547.png" alt="Facebook" width="40" style="display:block;margin:0 auto;border:0;height:auto;">
              <div style="margin-top:6px;font-size:0.7rem;color:#000000;text-align:center;">Facebook</div>
            </a>
          </td>
        </tr>
      </tbody>
    </table>
  </div>

  <div style="height:16px;"></div>
</div>
',
  'Sommernyt fra GamMa
{{ContentBlocks}}',
  'NEWSLETTER',
  1,
  '2026-08-26 09:00:00',
  '2026-08-26 09:00:00'
),
(
  'GamMa Event Template',
  'Kommende arrangementer fra GamMa',
  'Kommende arrangementer fra GamMa',
  '<!-- GammaEmailBlockDesign eventColor="#1f78c1" newsColor="#1877F2" -->
<!--
  GamMa event-mail template
  Rediger kun indholdet mellem:
  START: EVENTS
  END: EVENTS

  Arbejdsgang:
  1. Kopiér en event-blok for hvert arrangement.
  2. Udskift titel, brødtekst, knapper og links.
  3. Slet blokke, du ikke skal bruge.
-->

<!-- Preheader (hidden in most clients) -->
<div style="display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;">
  Kommende arrangementer fra GamMa
</div>

<!-- Outer wrapper -->
<div style="margin:0;padding:0;background-color:#eaf2fb;width:100%;font-family:Arial,sans-serif;">

  <!-- Banner -->
  <a href="https://gam-ma.dk/" target="_blank" style="text-decoration:none;display:block;">
    <img src="https://gam-ma.dk/lib/logo_blue.png"
         alt="GamMa banner"
         width="100%"
         style="display:block;border:0;outline:none;text-decoration:none;height:auto;max-height:220px;object-fit:contain;background-color:#ffffff;padding:16px 0;">
  </a>

  <!-- MAIN CONTAINER -->
  <div style="width:90%;max-width:720px;margin:16px auto 24px auto;background-color:#ffffff;
              padding:20px;color:#000000;line-height:1.55;font-size:0.95rem;
              box-shadow:0 6px 22px rgba(0,0,0,0.10);">

    <p style="margin:0 0 14px;"><strong>Kære GamMa-medlem,</strong></p>

    <p style="margin:0 0 18px;">
      Her kommer en kort opdatering med kommende arrangementer fra GamMa.
    </p>

    <!-- START: EVENTS -->

{{EventBlocks}}
{{NewsBlocks}}

<!-- END: EVENTS -->

    <p style="margin:18px 0 0;">
      Vi håber, du har lyst til at kigge forbi og tage en ven med, hvis du kender en GamMa''er, der også bør få det med.
    </p>

    <p style="margin:8px 0 0;">
      Med venlig hilsen<br>
      <strong>GamMas bestyrelse</strong>
    </p>

  </div>

  <div style="height:16px;"></div>
</div>

<div style="height:1px;background-color:#e6e6e6;width:90%;max-width:720px;margin:0 auto 12px auto;"></div>

<!-- Membership -->
<div style="width:90%;max-width:720px;margin:0 auto 24px auto;color:#000000;line-height:1.45;font-size:0.9rem;">
  <div style="padding:16px;background-color:#fffefd;border-radius:10px;border:1px solid #f1f1f1;">
    <p style="margin:0 0 8px;font-weight:bold;">Medlemskab?</p>
    <p style="margin:0 0 8px;">Husk, at deltagelse i GamMa-arrangementer kræver et aktivt medlemskab.</p>
    <p style="margin:0 0 8px;">
      Tjek betaling og opdater dine oplysninger på
      <a href="https://www.gam-ma.dk/Identity/Account/Manage" target="_blank" style="color:#337ab7;text-decoration:none;">
        GamMa.dk → <span style="font-weight:bold;">Profil</span>
      </a>.
    </p>
    <p style="margin:0 0 8px;">Bliv medlem sådan her:</p>
    <ul style="margin:0 0 10px;padding-left:20px;">
      <li>MobilePay: <strong>150 kr. til 22766</strong></li>
      <li>Log ind på <a href="https://gam-ma.dk/" style="color:#337ab7;text-decoration:none;">gam-ma.dk</a> og betal med kort under <em>Menu &gt;&gt; Betal</em></li>
      <li>Skan/tryk på QR-koden nedenfor</li>
    </ul>
    <p style="margin:0 0 10px;text-align:center;">
      <a href="https://mobilepay.dk/erhverv/betalingslink/betalingslink-svar?phone=22766&amp;amount=150&amp;comment=Medlemskab" target="_blank" style="text-decoration:none;">
        <img src="https://lh7-rt.googleusercontent.com/docsz/AD_4nXdzlpmoaRPEsXlhWfS76cmyJHopn0ji3CFV1v86x-DkurRa-LThEnuVntyyz_TKWq4tRvNRxZatoo88bb_rs3aaczNZWpWHxotuJ6quVDo4NXxpXu6ZEJsyEW1Ig5IR0xfB8fM8?key=AprQhRqq-JWjeqGFDCqqvMz9" alt="Medlemskab MobilePay QR" width="150" style="border:0;display:block;margin:0 auto;height:auto;">
      </a>
    </p>
    <p style="margin:0;text-align:center;font-size:0.75rem;">
      Ønsker du at blive afmeldt/udmeldt, så log ind på
      <a href="https://www.gam-ma.dk/Identity/Account/Manage" target="_blank" style="color:#337ab7;text-decoration:none;">GamMa.dk → Privatdata</a>
      og klik <b>''slet''</b>.
    </p>
  </div>
</div>

<div style="height:1px;background-color:#e6e6e6;width:90%;max-width:720px;margin:0 auto 12px auto;"></div>

<!-- Social icons -->
<div style="width:90%;max-width:720px;margin:0 auto 24px auto;text-align:center;background-color:#ffffff;border-radius:10px;">
  <table role="presentation" cellpadding="0" cellspacing="0" style="width:100%;border-collapse:collapse;border:none;background:none;">
    <tbody>
      <tr>
        <td style="padding:10px;border:none;background:none;">
          <a href="https://www.linkedin.com/company/gamma-math-ucph/" target="_blank" style="text-decoration:none;">
            <img src="https://cdn-icons-png.flaticon.com/512/174/174857.png" alt="LinkedIn" width="40" style="display:block;margin:0 auto;border:0;height:auto;">
            <div style="margin-top:6px;font-size:0.7rem;color:#000000;">LinkedIn</div>
          </a>
        </td>
        <td style="padding:10px;border:none;background:none;">
          <a href="https://www.instagram.com/gamma_ku_2100?igsh=MTFjYmk4M213djc0NA==" target="_blank" style="text-decoration:none;">
            <img src="https://cdn-icons-png.flaticon.com/512/2111/2111463.png" alt="Instagram" width="40" style="display:block;margin:0 auto;border:0;height:auto;">
            <div style="margin-top:6px;font-size:0.7rem;color:#000000;">Instagram</div>
          </a>
        </td>
        <td style="padding:10px;border:none;background:none;">
          <a href="https://www.facebook.com/share/g/1Exv7epudc/?mibextid=wwXIfr" target="_blank" style="text-decoration:none;">
            <img src="https://cdn-icons-png.flaticon.com/512/733/733547.png" alt="Facebook" width="40" style="display:block;margin:0 auto;border:0;height:auto;">
            <div style="margin-top:6px;font-size:0.7rem;color:#000000;">Facebook</div>
          </a>
        </td>
      </tr>
    </tbody>
  </table>
</div>

<div style="height:16px;"></div>
',
  'Kommende arrangementer fra GamMa
{{EventBlocks}}
{{NewsBlocks}}',
  'EVENT',
  1,
  '2026-08-20 10:30:00',
  '2026-08-20 10:30:00'
);

INSERT INTO `EmailTemplates` (`Name`, `Subject`, `Preheader`, `HtmlBody`, `TextBody`, `TemplateType`, `IsActive`, `Created`, `Updated`) VALUES
(
  'System - Opret ny bruger',
  'Bekræft din email',
  'Bekræft din GamMa-bruger',
  '<div style="display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;">{{Heading}}</div>
<div style="margin:0;padding:0;background-color:#eaf2fb;width:100%;font-family:Arial,sans-serif;color:#173042;">
  <a href="https://gam-ma.dk/" target="_blank" style="text-decoration:none;display:block;">
    <img src="https://gam-ma.dk/lib/logo_blue.png" alt="GamMa" width="100%" style="display:block;border:0;outline:none;text-decoration:none;height:auto;max-height:180px;object-fit:contain;background-color:#ffffff;padding:16px 0;">
  </a>
  <div style="width:90%;max-width:640px;margin:18px auto 22px auto;background-color:#ffffff;padding:28px 24px;line-height:1.55;font-size:15px;border:1px solid #d9e7f5;">
    <p style="margin:0 0 12px;">Hej <strong>{{Name}}</strong></p>
    <h1 style="margin:0 0 14px;font-size:28px;line-height:1.15;color:#10233a;">{{Heading}}</h1>
    <p style="margin:0 0 18px;">{{Intro}}</p>
    <a href="{{ActionUrl}}" target="_blank" style="display:block;width:100%;box-sizing:border-box;text-align:center;background-color:#2485c7;color:#ffffff;text-decoration:none;padding:13px 18px;font-size:15px;font-weight:bold;">{{ActionText}}</a>
    <p style="margin:18px 0 0;color:#4f6475;font-size:14px;">{{Note}}</p>
    <p style="margin:18px 0 0;">Med venlig hilsen<br><strong>GamMas bestyrelse</strong></p>
  </div>
  <div style="height:1px;background-color:#cfe0ef;width:90%;max-width:640px;margin:0 auto 12px auto;"></div>
  <div style="width:90%;max-width:640px;margin:0 auto 24px auto;text-align:center;background-color:#ffffff;">
    <table role="presentation" cellpadding="0" cellspacing="0" style="width:100%;border-collapse:collapse;border:none;background:none;"><tbody><tr>
      <td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.linkedin.com/company/gamma-math-ucph/" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/174/174857.png" alt="LinkedIn" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">LinkedIn</div></a></td>
      <td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.instagram.com/gamma_ku_2100?igsh=MTFjYmk4M213djc0NA==" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/2111/2111463.png" alt="Instagram" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">Instagram</div></a></td>
      <td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.facebook.com/share/g/1Exv7epudc/?mibextid=wwXIfr" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/733/733547.png" alt="Facebook" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">Facebook</div></a></td>
    </tr></tbody></table>
  </div>
  <div style="height:16px;"></div>
</div>',
  'Hej {{Name}}

{{Intro}}

{{ActionText}}: {{ActionUrl}}

{{Note}}

Med venlig hilsen
GamMas bestyrelse',
  'SYSTEM_REGISTRATION_CONFIRMATION',
  1,
  '2026-09-03 12:00:00',
  '2026-09-03 12:00:00'
),
(
  'System - Bekræft email',
  'Bekræft din email',
  'Bekræft din email hos GamMa',
  '<div style="display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;">{{Heading}}</div>
<div style="margin:0;padding:0;background-color:#eaf2fb;width:100%;font-family:Arial,sans-serif;color:#173042;">
  <a href="https://gam-ma.dk/" target="_blank" style="text-decoration:none;display:block;"><img src="https://gam-ma.dk/lib/logo_blue.png" alt="GamMa" width="100%" style="display:block;border:0;outline:none;text-decoration:none;height:auto;max-height:180px;object-fit:contain;background-color:#ffffff;padding:16px 0;"></a>
  <div style="width:90%;max-width:640px;margin:18px auto 22px auto;background-color:#ffffff;padding:28px 24px;line-height:1.55;font-size:15px;border:1px solid #d9e7f5;">
    <p style="margin:0 0 12px;">Hej <strong>{{Name}}</strong></p>
    <h1 style="margin:0 0 14px;font-size:28px;line-height:1.15;color:#10233a;">{{Heading}}</h1>
    <p style="margin:0 0 18px;">{{Intro}}</p>
    <a href="{{ActionUrl}}" target="_blank" style="display:block;width:100%;box-sizing:border-box;text-align:center;background-color:#2485c7;color:#ffffff;text-decoration:none;padding:13px 18px;font-size:15px;font-weight:bold;">{{ActionText}}</a>
    <p style="margin:18px 0 0;color:#4f6475;font-size:14px;">{{Note}}</p>
    <p style="margin:18px 0 0;">Med venlig hilsen<br><strong>GamMas bestyrelse</strong></p>
  </div>
  <div style="height:1px;background-color:#cfe0ef;width:90%;max-width:640px;margin:0 auto 12px auto;"></div>
  <div style="width:90%;max-width:640px;margin:0 auto 24px auto;text-align:center;background-color:#ffffff;"><table role="presentation" cellpadding="0" cellspacing="0" style="width:100%;border-collapse:collapse;border:none;background:none;"><tbody><tr><td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.linkedin.com/company/gamma-math-ucph/" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/174/174857.png" alt="LinkedIn" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">LinkedIn</div></a></td><td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.instagram.com/gamma_ku_2100?igsh=MTFjYmk4M213djc0NA==" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/2111/2111463.png" alt="Instagram" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">Instagram</div></a></td><td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.facebook.com/share/g/1Exv7epudc/?mibextid=wwXIfr" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/733/733547.png" alt="Facebook" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">Facebook</div></a></td></tr></tbody></table></div>
  <div style="height:16px;"></div>
</div>',
  'Hej {{Name}}

{{Intro}}

{{ActionText}}: {{ActionUrl}}

{{Note}}

Med venlig hilsen
GamMas bestyrelse',
  'SYSTEM_EMAIL_CONFIRMATION',
  1,
  '2026-09-03 12:00:00',
  '2026-09-03 12:00:00'
),
(
  'System - Ændr email',
  'Bekræft email',
  'Bekræft din nye email hos GamMa',
  '<div style="display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;">{{Heading}}</div>
<div style="margin:0;padding:0;background-color:#eaf2fb;width:100%;font-family:Arial,sans-serif;color:#173042;">
  <a href="https://gam-ma.dk/" target="_blank" style="text-decoration:none;display:block;"><img src="https://gam-ma.dk/lib/logo_blue.png" alt="GamMa" width="100%" style="display:block;border:0;outline:none;text-decoration:none;height:auto;max-height:180px;object-fit:contain;background-color:#ffffff;padding:16px 0;"></a>
  <div style="width:90%;max-width:640px;margin:18px auto 22px auto;background-color:#ffffff;padding:28px 24px;line-height:1.55;font-size:15px;border:1px solid #d9e7f5;"><p style="margin:0 0 12px;">Hej <strong>{{Name}}</strong></p><h1 style="margin:0 0 14px;font-size:28px;line-height:1.15;color:#10233a;">{{Heading}}</h1><p style="margin:0 0 18px;">{{Intro}}</p><a href="{{ActionUrl}}" target="_blank" style="display:block;width:100%;box-sizing:border-box;text-align:center;background-color:#2485c7;color:#ffffff;text-decoration:none;padding:13px 18px;font-size:15px;font-weight:bold;">{{ActionText}}</a><p style="margin:18px 0 0;color:#4f6475;font-size:14px;">{{Note}}</p><p style="margin:18px 0 0;">Med venlig hilsen<br><strong>GamMas bestyrelse</strong></p></div>
  <div style="height:1px;background-color:#cfe0ef;width:90%;max-width:640px;margin:0 auto 12px auto;"></div>
  <div style="width:90%;max-width:640px;margin:0 auto 24px auto;text-align:center;background-color:#ffffff;"><table role="presentation" cellpadding="0" cellspacing="0" style="width:100%;border-collapse:collapse;border:none;background:none;"><tbody><tr><td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.linkedin.com/company/gamma-math-ucph/" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/174/174857.png" alt="LinkedIn" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">LinkedIn</div></a></td><td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.instagram.com/gamma_ku_2100?igsh=MTFjYmk4M213djc0NA==" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/2111/2111463.png" alt="Instagram" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">Instagram</div></a></td><td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.facebook.com/share/g/1Exv7epudc/?mibextid=wwXIfr" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/733/733547.png" alt="Facebook" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">Facebook</div></a></td></tr></tbody></table></div><div style="height:16px;"></div></div>',
  'Hej {{Name}}

{{Intro}}

{{ActionText}}: {{ActionUrl}}

{{Note}}

Med venlig hilsen
GamMas bestyrelse',
  'SYSTEM_EMAIL_CHANGE_CONFIRMATION',
  1,
  '2026-09-03 12:00:00',
  '2026-09-03 12:00:00'
),
(
  'System - Nulstil password',
  'Nulstil Password',
  'Nulstil dit password hos GamMa',
  '<div style="display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;">{{Heading}}</div>
<div style="margin:0;padding:0;background-color:#eaf2fb;width:100%;font-family:Arial,sans-serif;color:#173042;"><a href="https://gam-ma.dk/" target="_blank" style="text-decoration:none;display:block;"><img src="https://gam-ma.dk/lib/logo_blue.png" alt="GamMa" width="100%" style="display:block;border:0;outline:none;text-decoration:none;height:auto;max-height:180px;object-fit:contain;background-color:#ffffff;padding:16px 0;"></a><div style="width:90%;max-width:640px;margin:18px auto 22px auto;background-color:#ffffff;padding:28px 24px;line-height:1.55;font-size:15px;border:1px solid #d9e7f5;"><p style="margin:0 0 12px;">Hej <strong>{{Name}}</strong></p><h1 style="margin:0 0 14px;font-size:28px;line-height:1.15;color:#10233a;">{{Heading}}</h1><p style="margin:0 0 18px;">{{Intro}}</p><a href="{{ActionUrl}}" target="_blank" style="display:block;width:100%;box-sizing:border-box;text-align:center;background-color:#2485c7;color:#ffffff;text-decoration:none;padding:13px 18px;font-size:15px;font-weight:bold;">{{ActionText}}</a><p style="margin:18px 0 0;color:#4f6475;font-size:14px;">{{Note}}</p><p style="margin:18px 0 0;">Med venlig hilsen<br><strong>GamMas bestyrelse</strong></p></div><div style="height:1px;background-color:#cfe0ef;width:90%;max-width:640px;margin:0 auto 12px auto;"></div><div style="width:90%;max-width:640px;margin:0 auto 24px auto;text-align:center;background-color:#ffffff;"><table role="presentation" cellpadding="0" cellspacing="0" style="width:100%;border-collapse:collapse;border:none;background:none;"><tbody><tr><td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.linkedin.com/company/gamma-math-ucph/" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/174/174857.png" alt="LinkedIn" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">LinkedIn</div></a></td><td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.instagram.com/gamma_ku_2100?igsh=MTFjYmk4M213djc0NA==" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/2111/2111463.png" alt="Instagram" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">Instagram</div></a></td><td style="padding:10px;border:none;background:none;text-align:center;"><a href="https://www.facebook.com/share/g/1Exv7epudc/?mibextid=wwXIfr" target="_blank" style="text-decoration:none;display:inline-block;text-align:center;"><img src="https://cdn-icons-png.flaticon.com/512/733/733547.png" alt="Facebook" width="36" style="display:block;margin:0 auto;border:0;height:auto;"><div style="margin-top:6px;font-size:12px;color:#000000;text-align:center;">Facebook</div></a></td></tr></tbody></table></div><div style="height:16px;"></div></div>',
  'Hej {{Name}}

{{Intro}}

{{ActionText}}: {{ActionUrl}}

{{Note}}

Med venlig hilsen
GamMas bestyrelse',
  'SYSTEM_PASSWORD_RESET',
  1,
  '2026-09-03 12:00:00',
  '2026-09-03 12:00:00'
);
