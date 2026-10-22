import { InfoLayout } from "../layouts/InfoLayout.jsx";
import { Link } from "../routes/navigation.jsx";

const statutesUrl = "https://gamma-math.github.io/gammastatic/vedtaegter-gamma.pdf";
const cvrUrl = "https://datacvr.virk.dk/data/visenhed?enhedstype=virksomhed&id=34637768";
const subscriptionsUrl = "/react/account/manage/subscriptions";

const termsSections = [
  { id: "privatlivspolitik", label: "Privatlivspolitik" },
  { id: "brugerbetingelser", label: "Brugerbetingelser" },
  { id: "medlemsbetingelser", label: "Betingelser for medlemskab" },
  { id: "salgsbetingelser", label: "Salgsbetingelser" }
];

const pages = {
  about: {
    title: "Om GamMa",
    kicker: "Foreningen",
    body: (
      <>
        <p>GamMa er alumneforeningen for matematikere fra Københavns Universitet.</p>
        <p>React-siden her erstatter de almindelige informationssider, mens login, roller og backend fortsat ligger i ASP.NET Core.</p>
      </>
    )
  },
  cookies: {
    title: "Cookies",
    kicker: "Privatliv",
    body: (
      <section className="static-copy-section">
        <p>Alumneforeningen GamMa indsamler cookies i forbindelse med login på denne side. Ved login gives accept til, at dette må finde sted.</p>
        <p>De indsamlede cookies anvendes alene til at muliggøre login på hjemmesiden.</p>
        <p>GamMa benytter ikke information fra cookies til kommercielle eller statistiske formål, og data relateret til cookies videregives ikke til tredjeparter.</p>
        <p>Denne hjemmesides loginsystem er baseret på <a href="https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity" target="_blank" rel="noreferrer">ASP.NET Core Identity</a>, som kræver brug af cookies.</p>
      </section>
    )
  }
};

/**
 * Renders the shared public information pages.
 */
export function StaticPage({ page }) {
  if (page === "terms") {
    return <TermsPage />;
  }

  const content = pages[page] ?? pages.about;

  return (
    <main className="frontpage-main">
      <section className="page-shell frontpage-section static-copy">
        <div className="static-copy-hero">
          <p className="menu-section-title">{content.kicker}</p>
          <h1>{content.title}</h1>
        </div>
        <div className="static-copy-body">{content.body}</div>
      </section>
    </main>
  );
}

function TermsPage() {
  return (
    <InfoLayout
      title="Betingelser og vedtægter"
      kicker="Betingelser og vedtægter"
      sections={termsSections}
    >
      <InfoSection id="privatlivspolitik" title="Privatlivspolitik">
        <p>Alumneforeningen GamMa behandler de oplysninger, du giver os, for at kunne administrere medlemskab, login, kommunikation og foreningens aktiviteter.</p>
        <p>Det kan blandt andet være navn, årgang, telefonnummer, e-mailadresse, arbejde og adresse. Oplysningerne bruges kun til foreningsrelaterede formål og deles ikke til kommerciel brug.</p>
        <p>Du kan kontakte bestyrelsen på <a href="mailto:bestyrelsen@gam-ma.dk">bestyrelsen@gam-ma.dk</a>, hvis du har spørgsmål til dine oplysninger eller ønsker at få dem rettet.</p>
        <p>Oplysninger om cookies og login findes på <Link href="/react/cookies">cookies-siden</Link>.</p>
      </InfoSection>

      <InfoSection id="brugerbetingelser" title="Brugerbetingelser">
        <p>Ved oprettelse af en GamMa-profil accepterer du, at relevante medlemsoplysninger kan være tilgængelige for GamMas øvrige medlemmer i overensstemmelse med dine synlighedsindstillinger.</p>
        <ul>
          <li>Navn</li>
          <li>Årgang</li>
          <li>Telefonnummer</li>
          <li>E-mailadresse</li>
          <li>Arbejde</li>
          <li>Adresse</li>
        </ul>
        <p>Du kan til enhver tid ændre eller skjule oplysninger via <Link href="/react/account/manage">din profil</Link>. GamMas bestyrelse kan kontakte medlemmer via de kontaktoplysninger, der er registreret på profilen.</p>
        <p>GamMa videredistribuerer ikke dine oplysninger til kommerciel brug eller til tredjeparter, der ikke er direkte relateret til <a href="https://www.math.ku.dk/" target="_blank" rel="noreferrer">Institut for Matematiske Fag, Københavns Universitet</a>.</p>
      </InfoSection>

      <InfoSection id="medlemsbetingelser" title="Betingelser for medlemskab">
        <p>For at kunne blive medlem af foreningen skal du opfylde et af kriterierne i § 4 i foreningens vedtægter:</p>
        <ol>
          <li>Du har opnået en bachelor-, kandidat- og/eller PhD-grad fra Institut for Matematiske Fag ved Københavns Universitet.</li>
          <li>Du er indskrevet på kandidat- eller PhD-studiet ved Institut for Matematiske Fag ved Københavns Universitet.</li>
          <li>Du har opnået en grad ved et universitet og bestået mindst 90 ECTS-point fra Institut for Matematiske Fag ved Københavns Universitet.</li>
        </ol>
        <p>De gældende priser vises på betalingssiden. Som udgangspunkt er årskontingentet 150 kr. for dimittender, mens kandidatstuderende kan være kontingentfri efter foreningens gældende regler.</p>
        <p>Læs foreningens <a href={statutesUrl} target="_blank" rel="noreferrer">vedtægter</a> for de fulde regler om medlemskab.</p>
      </InfoSection>

      <InfoSection id="salgsbetingelser" title="Salgsbetingelser">
        <p>Disse salgsbetingelser gælder ved betaling for medlemskab og betalingspligtige arrangementer hos Alumneforeningen GamMa, <a href={cvrUrl} target="_blank" rel="noreferrer">CVR 34637768</a>. Ved betaling indgår du en aftale med GamMa om det medlemskab eller den eventplads, der er beskrevet på den relevante side.</p>
        <p>GamMas aktuelle virksomhedsoplysninger, herunder registreret adresse, kan findes via CVR-registret.</p>

        <h3>1. Oplysninger om aftalen</h3>
        <p>Før betaling skal den relevante ydelse, pris, medlemsperiode eller eventuelle dato, sted og andre væsentlige oplysninger fremgå klart. Hvis du vælger automatisk fornyelse, skal det også fremgå, at der er tale om en løbende betalingsaftale, hvilket beløb og betalingsinterval der gælder, og hvordan aftalen opsiges. Eventuelle særlige vilkår på den konkrete eventside supplerer disse salgsbetingelser og gælder foran disse, hvis de tydeligt angiver andet.</p>
        <p>Aftalen indgås på dansk. Når betalingen er gennemført, vises en bekræftelse på hjemmesiden. Du bør gemme bekræftelsen og eventuelle oplysninger om arrangementet.</p>

        <h3>2. Medlemskab</h3>
        <p>Et medlemskab giver adgang til de medlemsfordele, som GamMa til enhver tid tilbyder, og gælder for den medlemsperiode, der fremgår af medlemskabsoplysningerne. Medlemskab forudsætter, at du opfylder foreningens medlemskriterier og accepterer de gældende vedtægter og brugerbetingelser.</p>
        <p>Betaler du én gang, fornyes medlemskabet ikke automatisk. Der oprettes kun en løbende betalingsaftale, hvis du aktivt vælger automatisk fornyelse og udtrykkeligt godkender den hos den valgte betalingsudbyder.</p>
        <p>Ved automatisk fornyelse fornyes medlemskabet for den periode, der er oplyst ved tilmeldingen. Den nuværende medlemsopsætning er et år ad gangen til 150 kr. pr. år. Beløbet opkræves automatisk via den valgte betalingsmetode ved begyndelsen af den næste medlemsperiode.</p>
        <p>GamMa kan med mindst én måneds varsel ændre medlemskabsbeløbet, hvis ændringen er besluttet på en generalforsamling. Ved en sådan ændring vil automatiske betalingsaftaler enten blive opsagt, eller det nye beløb blive opkrævet fremover.</p>
        <p>Du kan til enhver tid opsige den automatiske betalingsaftale på <Link href={subscriptionsUrl}>siden Mine abonnementer</Link>. Du bør altid opsige aftalen på hjemmesiden, da det også opsiger aftalen hos den eksterne betalingsudbyder. Opsigelsen tilbagefører ikke automatisk en allerede betalt periode og ændrer ikke den allerede betalte medlemsadgang eller registreringen af perioden som betalt. Hvis du ønsker tilbagebetaling, gælder reglerne i afsnit 5.</p>
        <p>At tilbagekalde samtykke til fremtidige betalinger hos banken eller en ekstern betalingsudbyder er ikke det samme som at opsige medlemsaftalen hos GamMa. En tilbagekaldelse ændrer ikke i sig selv reglerne om en allerede gennemført betaling, medlemsperioden eller eventuel tilbagebetaling.</p>

        <h3>3. Arrangementer og eventtilmelding</h3>
        <p>En betalt eventtilmelding gælder for det konkrete arrangement, den dato eller periode og eventuelt den deltager, som fremgår af bestillingen. Tilmeldingen er personlig, medmindre eventbeskrivelsen udtrykkeligt giver mulighed for overdragelse.</p>
        <p>GamMa kan ændre praktiske oplysninger om et arrangement, hvis ændringen ikke væsentligt ændrer arrangementets karakter. Hvis GamMa aflyser arrangementet eller ikke kan levere den købte eventplads, tilbagebetaler GamMa det betalte beløb.</p>

        <h3>4. Pris og betaling</h3>
        <p>Den pris, medlemsperiode og eventuelle automatiske betalingsaftale, der gælder for købet, skal vises tydeligt før betaling. Ved automatisk fornyelse er den oplyste pris og det oplyste betalingsinterval grundlaget for de kommende opkrævninger, indtil aftalen opsiges eller ændres efter gældende regler.</p>
        <p>Betaling gennemføres med de betalingsmetoder, der vises på betalingssiden, når disse er tilgængelige. Et medlemskab eller en eventtilmelding er først registreret som betalt, når den eksterne betalingsudbyder har bekræftet betalingen. Hvis betalingen afvises, annulleres eller ikke gennemføres, er der ikke indgået en betalt aftale.</p>

        <h3>5. Fortrydelse, afbud og tilbagebetaling</h3>
        <p>For medlemskab købt online er fortrydelsesfristen 14 dage fra aftalens indgåelse, medmindre andet følger af ufravigelig lovgivning. Hvis du ønsker at gøre brug af fortrydelsesretten, skal du kontakte bestyrelsen på <a href="mailto:bestyrelsen@gam-ma.dk">bestyrelsen@gam-ma.dk</a> inden fristens udløb.</p>
        <p>For betalte arrangementer tilbyder GamMa en frivillig fortrydelsesordning på 14 dage fra aftalens indgåelse. Fortrydelsen skal dog altid meddeles til bestyrelsen på <a href="mailto:bestyrelsen@gam-ma.dk">bestyrelsen@gam-ma.dk</a> senest 7 dage før arrangementets afholdelse. Efter den angivne frist eller efter arrangementets afholdelse kan der ikke gøres brug af fortrydelsesordningen.</p>
        <p>Eventbeskrivelsen oplyser, hvis der gælder særlige regler for afbud eller navneændring. Tilbagebetaling sker, når afmeldingen er meddelt inden den frist, der er angivet i disse salgsbetingelser eller i eventbeskrivelsen.</p>

        <h3>6. Fejl og manglende levering</h3>
        <p>Hvis et arrangement aflyses, ikke gennemføres som beskrevet eller har væsentlige mangler, skal du kontakte GamMa hurtigst muligt på <a href="mailto:bestyrelsen@gam-ma.dk">bestyrelsen@gam-ma.dk</a>. GamMa vurderer sagen og tilbyder ombooking eller hel eller delvis tilbagebetaling, hvis det følger af aftalen eller gældende lovgivning.</p>

        <h3>7. Ansvar</h3>
        <p>GamMa er ikke ansvarlig for manglende opfyldelse, der skyldes forhold uden for foreningens rimelige kontrol, herunder myndighedsforhold, strejke, sygdom, brand, naturkatastrofer, tekniske driftsforstyrrelser eller andre force majeure-forhold. Bestemmelsen begrænser ikke rettigheder, som ikke lovligt kan begrænses.</p>

        <h3>8. Kontakt og klager</h3>
        <p>Spørgsmål, indsigelser og klager vedrørende betaling, medlemskab eller arrangementer kan sendes til <a href="mailto:bestyrelsen@gam-ma.dk">bestyrelsen@gam-ma.dk</a>. Oplys gerne navn, ordrebekræftelse og en beskrivelse af sagen, så GamMa kan behandle henvendelsen.</p>

        <h3>9. Lovvalg og ændringer</h3>
        <p>Salgsbetingelserne er underlagt dansk ret. Ufravigelige regler, der beskytter forbrugere, gælder uanset dette. Salgsbetingelserne gælder i den version, der var tilgængelig på købstidspunktet. Ændringer gælder ikke for allerede indgåede aftaler, medmindre andet følger af lovgivningen.</p>

        <p>Senest opdateret: 20. oktober 2026.</p>
      </InfoSection>
    </InfoLayout>
  );
}

function InfoSection({ id, title, children }) {
  return (
    <section className="info-page-section" id={id}>
      <h2>{title}</h2>
      {children}
    </section>
  );
}
