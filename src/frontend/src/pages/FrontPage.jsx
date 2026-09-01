import { useEffect, useState } from "react";
import { ContentCard } from "../components/ContentCard.jsx";
import { MenuLayout } from "../layouts/MenuLayout.jsx";
import { Link } from "../routes/navigation.jsx";
import { contentApi } from "../services/api.js";

export function FrontPage({ mode, title }) {
  const [items, setItems] = useState([]);
  const [error, setError] = useState("");

  useEffect(() => {
    contentApi.listPublished(mode)
      .then(setItems)
      .catch((err) => setError(err.message));
  }, [mode]);

  if (mode) {
    return (
      <MenuLayout active={mode === "EVENT" ? "/react/events" : "/react/news"}>
        <div className="menu-panel-header">
          <div>
            <p className="menu-section-title">{title}</p>
            <p className="menu-panel-lead">
              {mode === "EVENT"
                ? "Se kommende arrangementer."
                : "Se de seneste historier, opdateringer og indblik fra foreningen."}
            </p>
          </div>
        </div>
        <div className="menu-event-rail-head">
          <button className="menu-arrow-button" type="button" aria-label="Forrige">&lt;</button>
          <button className="menu-arrow-button" type="button" aria-label="Næste">&gt;</button>
        </div>
        {error && <p className="status-message">{error}</p>}
        <div className="menu-event-grid menu-event-grid-three">
          {items.map((item) => <ContentCard item={item} key={item.id} />)}
        </div>
      </MenuLayout>
    );
  }

  const featured = items.slice(0, 4);

  return (
    <main className="page-shell frontpage-main">
      <section className="frontpage-intro">
        <div className="frontpage-intro-grid">
          <div className="frontpage-section-copy">
            <h1>Alumneforeningen GamMa</h1>
            <ul className="frontpage-intro-list">
              <li>For Gamle Matematikere og andre med tilknytning til de matematiske fag.</li>
              <li>For både forhenværende og nuværende studerende.</li>
              <li>For medlemmer med bachelor- og/eller kandidatgrad fra Københavns Universitet.</li>
            </ul>
          </div>
          <aside className="frontpage-membership-card">
            <h2>Medlemskab</h2>
            <ul className="frontpage-membership-list">
              <li><span>Færdiguddannet</span><strong>150 kr./ årligt</strong></li>
              <li><span>Studerende</span><strong>Gratis</strong></li>
            </ul>
          </aside>
        </div>
      </section>

      <section className="frontpage-section frontpage-news">
        <div className="frontpage-section-head">
          <h2>Nyheder og events</h2>
          <Link className="frontpage-button frontpage-button-secondary" href="/react/news">Se mere</Link>
        </div>
        {error && <p className="status-message">{error}</p>}
        <div className="frontpage-news-grid">
          {featured.map((item) => <ContentCard item={item} variant="frontpage" key={item.id} />)}
        </div>
      </section>
    </main>
  );
}
