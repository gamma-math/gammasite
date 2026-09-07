import { adminItems, Link } from "../routes/navigation.jsx";

/**
 * Shared admin layout that hides write-only sections from read-only admins.
 */
export function AdminLayout({ active, canWrite, children }) {
  return (
    <main className="menu-shell">
      <section className="menu-workspace">
        <aside className="menu-sidebar">
          <h2 className="menu-sidebar-title">Admin</h2>
          <nav className="menu-side-nav" aria-label="Admin sektioner">
            {adminItems.map((item) => {
              if (!canWrite && !item.readAdmin) {
                return null;
              }
              return (
                <Link className={`menu-side-link ${active === item.href ? "is-active" : ""}`} href={item.href} key={item.href}>
                  {item.label}
                </Link>
              );
            })}
          </nav>
        </aside>
        <section className="menu-content">{children}</section>
      </section>
    </main>
  );
}
