import { useEffect, useState } from "react";
import { SidebarNav } from "../components/SidebarNav.jsx";

/**
 * Two-column layout for public information pages with in-page section navigation.
 */
export function InfoLayout({ title, kicker, sections, children }) {
  const firstSection = sections[0]?.id ?? "";
  const [active, setActive] = useState(getHashSection(firstSection));

  useEffect(() => {
    const updateFromHash = () => {
      const sectionId = getHashSection(firstSection);
      setActive(sectionId);

      if (window.location.hash) {
        window.requestAnimationFrame(() => {
          document.getElementById(sectionId)?.scrollIntoView({ block: "start" });
        });
      }
    };

    updateFromHash();
    window.addEventListener("hashchange", updateFromHash);
    return () => window.removeEventListener("hashchange", updateFromHash);
  }, [firstSection]);

  function selectSection(event, sectionId) {
    event.preventDefault();
    setActive(sectionId);
    window.history.pushState({}, "", `${window.location.pathname}#${sectionId}`);
    document.getElementById(sectionId)?.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  const navSections = [{ items: sections.map(({ id, label }) => ({ id, label, href: `#${id}` })) }];

  return (
    <main className="menu-shell info-shell">
      <section className="menu-workspace">
        <SidebarNav
          title="Information"
          sections={navSections}
          active={active}
          ariaLabel="Information om medlemskab"
          renderLink={(item, className, isActive) => (
            <a
              className={className}
              href={item.href}
              key={item.href}
              onClick={(event) => selectSection(event, item.id)}
              aria-current={isActive ? "location" : undefined}
            >
              {item.label}
            </a>
          )}
        />
        <article className="menu-content info-content">
          <header className="info-page-hero">
            <p className="menu-section-title">{kicker}</p>
            <h1>{title}</h1>
            <p className="info-page-intro">Her finder du information om GamMas medlemskab, brugerforhold og betaling.</p>
          </header>
          <div className="info-page-body">{children}</div>
        </article>
      </section>
    </main>
  );
}

function getHashSection(fallback) {
  return window.location.hash.replace(/^#/, "") || fallback;
}
