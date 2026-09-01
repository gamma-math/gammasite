import { Link } from "../routes/navigation.jsx";

const pages = {
  about: {
    title: "Om GamMa",
    kicker: "Foreningen",
    body: [
      "GamMa er alumneforeningen for matematikere fra Københavns Universitet.",
      "React-siden her erstatter de almindelige informationssider, mens login, roller og backend fortsat ligger i ASP.NET Core."
    ]
  },
  terms: {
    title: "Betingelser og vedtægter",
    kicker: "Jura",
    body: [
      "Her samles betingelser for medlemskab og betaling.",
      "De eksisterende dokumentlinks kan kobles på som indhold i biblioteket eller som ContentItems i næste iteration."
    ]
  },
  cookies: {
    title: "Cookies",
    kicker: "Privatliv",
    body: [
      "Siden bruger cookies til ASP.NET Identity-login og sessionsbaserede funktioner.",
      "React-frontenden genbruger samme cookies og introducerer ikke JWT-login."
    ]
  }
};

export function StaticPage({ page }) {
  const content = pages[page] ?? pages.about;

  return (
    <main className="frontpage-main">
      <section className="page-shell frontpage-section static-copy">
        <p className="menu-section-title">{content.kicker}</p>
        <h1>{content.title}</h1>
        {content.body.map((paragraph) => <p key={paragraph}>{paragraph}</p>)}
        <Link className="frontpage-button frontpage-button-secondary" href="/react">Til forsiden</Link>
      </section>
    </main>
  );
}
