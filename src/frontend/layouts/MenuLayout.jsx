import { Link } from "../routes/navigation.jsx";

const userItems = [
  { href: "/react/events", label: "Begivenheder" },
  { href: "/react/news", label: "Nyheder" },
  { href: "/react/members", label: "Medlemmer", requiresAuth: true },
  { href: "/react/pay", label: "Betal medlemskab", requiresAuth: true }
];

export function MenuLayout({ active, title = "Menu", isAuthenticated = false, contentClassName = "", extraItems = [], children }) {
  const items = [...userItems, ...extraItems].filter((item) => !item.requiresAuth || isAuthenticated);

  return (
    <main className="menu-shell">
      <section className="menu-workspace">
        <aside className="menu-sidebar">
          <h2 className="menu-sidebar-title">{title}</h2>
          <nav className="menu-side-nav" aria-label="Brugersektioner">
            {items.map((item) => (
              <Link className={`menu-side-link ${active === item.href ? "is-active" : ""}`} href={item.href} key={item.href}>
                {item.label}
              </Link>
            ))}
          </nav>
        </aside>
        <section className={`menu-content ${contentClassName}`.trim()}>{children}</section>
      </section>
    </main>
  );
}
