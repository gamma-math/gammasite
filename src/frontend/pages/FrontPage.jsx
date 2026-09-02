import { useEffect, useState } from "react";
import { ContentCard } from "../components/ContentCard.jsx";
import { MenuLayout } from "../layouts/MenuLayout.jsx";
import { Link } from "../routes/navigation.jsx";
import { contentApi } from "../services/api.js";

export function FrontPage({ mode, title, user }) {
  const [items, setItems] = useState([]);
  const [error, setError] = useState("");
  const [pageIndex, setPageIndex] = useState(0);

  useEffect(() => {
    contentApi.listPublished(mode)
      .then(setItems)
      .catch((err) => setError(err.message));
  }, [mode]);

  useEffect(() => {
    setPageIndex(0);
  }, [mode, items.length]);

  const sortedItems = [...items].sort((left, right) => contentDateValue(right) - contentDateValue(left));

  if (mode) {
    const pageSize = 3;
    const pageCount = Math.max(1, Math.ceil(sortedItems.length / pageSize));
    const currentPage = Math.min(pageIndex, pageCount - 1);
    const visibleItems = sortedItems.slice(currentPage * pageSize, currentPage * pageSize + pageSize);

    return (
      <MenuLayout active={mode === "EVENT" ? "/react/events" : "/react/news"} isAuthenticated={user?.isAuthenticated}>
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
          <button className="menu-arrow-button" type="button" aria-label="Forrige" disabled={currentPage === 0} onClick={() => setPageIndex((page) => Math.max(0, page - 1))}>&lt;</button>
          <button className="menu-arrow-button" type="button" aria-label="Næste" disabled={currentPage >= pageCount - 1} onClick={() => setPageIndex((page) => Math.min(pageCount - 1, page + 1))}>&gt;</button>
        </div>
        {error && <p className="status-message">{error}</p>}
        <div className="menu-event-grid menu-event-grid-three">
          {visibleItems.map((item) => <ContentCard item={item} key={item.id} />)}
        </div>
      </MenuLayout>
    );
  }

  const featured = sortedItems.slice(0, 4);

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
          <Link className="frontpage-button frontpage-button-secondary" href="/react/events">Se mere</Link>
        </div>
        {error && <p className="status-message">{error}</p>}
        <div className="frontpage-news-grid">
          {featured.map((item) => <ContentCard item={item} variant="frontpage" key={item.id} />)}
        </div>
      </section>
    </main>
  );
}

function contentDateValue(item) {
  const value = item.type === "EVENT" ? item.startDate : item.publishedAt;
  return value ? new Date(value).getTime() : 0;
}
