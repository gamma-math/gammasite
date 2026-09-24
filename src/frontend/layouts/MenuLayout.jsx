import { Link } from "../routes/navigation.jsx";

const userItems = [
  { href: "/react/events", label: "Begivenheder" },
  { href: "/react/news", label: "Nyheder" }
];

const memberAreaItems = [
  { href: "/react/members", label: "Medlemmer", requiresAuth: true },
  { href: "/react/pay", label: "Betal medlemskab", requiresAuth: true },
  { href: "/react/bibliotek", label: "Bibliotek", requiresAuth: true },
  { href: "/react/finance", label: "Finans", requiresAuth: true }
];

/**
 * Shared two-column layout for member-facing React pages.
 */
export function MenuLayout({ active, title = "Menu", isAuthenticated = false, contentClassName = "", extraItems = [], includeDefaultItems = true, children }) {
  const visibleItems = (items) => items.filter((item) => !item.requiresAuth || isAuthenticated);
  const sections = [
    ...(includeDefaultItems ? [{ items: visibleItems(userItems) }, { label: "Medlemsområde", items: visibleItems(memberAreaItems) }] : []),
    ...(extraItems.length ? [{ items: visibleItems(extraItems) }] : [])
  ].filter((section) => section.items.length);

  return (
    <main className="menu-shell">
      <section className="menu-workspace">
        <aside className="menu-sidebar">
          <h2 className="menu-sidebar-title">{title}</h2>
          <nav className="menu-side-nav" aria-label="Brugersektioner">
            {sections.map((section, index) => (
              <div className="menu-nav-section" key={section.label ?? `section-${index}`}>
                {section.label && <p className="menu-nav-section-title">{section.label}</p>}
                {section.items.map((item) => (
                  <Link className={`menu-side-link ${active === item.href ? "is-active" : ""}`} href={item.href} key={item.href}>
                    {item.label}
                  </Link>
                ))}
              </div>
            ))}
          </nav>
        </aside>
        <section className={`menu-content ${contentClassName}`.trim()}>{children}</section>
      </section>
    </main>
  );
}
