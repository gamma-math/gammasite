const statutesUrl = "https://gamma-math.github.io/gammastatic/vedtaegter-gamma.pdf";

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
  terms: {
    title: "Betingelser og vedtægter",
    kicker: "Betingelser og vedtægter",
    body: (
      <>
        <section className="static-copy-section">
          <h2>Vedtægter</h2>
          <p>Du kan læse foreningens vedtægter <a href={statutesUrl} target="_blank" rel="noreferrer">her</a>.</p>
        </section>

        <section className="static-copy-section">
          <h2>Betingelser for medlemskab</h2>
          <p>For at kunne blive medlem af foreningen, skal du opfylde følgende kriterium fra §4 i foreningens vedtægter:</p>
          <p><em>Følgende personer kan optages i foreningen:</em></p>
          <ol>
            <li><em>Personer der har opnået en bachelor-, kandidat- og/eller PhD-grad fra Institut for Matematiske Fag - Københavns Universitet.</em></li>
            <li><em>Personer der er indskrevet på kandidat- eller PhD-studiet ved Institut for Matematiske Fag - Københavns Universitet.</em></li>
            <li><em>Personer der har opnået en grad ved et hvilket som helst universitet og bestået 90 ECTS-point fra Institut for Matematiske Fag - Københavns Universitet.</em></li>
          </ol>
          <p>Årskontingentet i GamMa er kr. 150,- for dimittender. Såfremt du melder dig ind i dag for første gang, vil medlemskab for resten af året kun koste kr. 75,-. Hvis du er kandidatstuderende er medlemskab gratis.</p>
        </section>

        <section className="static-copy-section">
          <h2>Brugerbetingelser</h2>
          <p>Ved oprettelse af GamMa-medlemskab på denne side accepteres det, at følgende informationer er tilgængelige for GamMas øvrige medlemmer:</p>
          <ul>
            <li>Navn</li>
            <li>Årgang</li>
            <li>Telefonnummer</li>
            <li>Mail</li>
            <li>Arbejde</li>
            <li>Adresse</li>
          </ul>
          <p><a href="/Identity/Account/Manage">Denne side</a> kan til enhver tid besøges for at regulere disse informationer.</p>
          <p>Specielt kan informationer udelades og skjules, hvis dette ønskes.</p>
          <p>GamMas bestyrelse forbeholder sig retten til at kontakte medlemmer via ovenstående oplysninger.</p>
          <p>GamMa videredistribuerer ikke dine informationer til kommerciel brug eller til tredjeparter, der ikke er direkte relateret til <a href="https://www.math.ku.dk/">Institut for Matematiske Fag, Københavns Universitet</a>.</p>
        </section>

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

export function StaticPage({ page }) {
  const content = pages[page] ?? pages.about;

  return (
    <main className="frontpage-main">
      <section className="page-shell frontpage-section static-copy">
        <div className="static-copy-hero">
          <p className="menu-section-title">{content.kicker}</p>
          <h1>{content.title}</h1>
        </div>
        <div className="static-copy-body">
          {content.body}
        </div>
      </section>
    </main>
  );
}
