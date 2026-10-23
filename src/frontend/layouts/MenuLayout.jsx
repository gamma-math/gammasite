import { SidebarNav } from "../components/SidebarNav.jsx";

const userItems = [
  { href: "/events", label: "Begivenheder" },
  { href: "/news", label: "Nyheder" }
];

const memberAreaItems = [
  { href: "/members", label: "Medlemmer", requiresAuth: true },
  { href: "/pay", label: "Betal medlemskab", requiresAuth: true },
  { href: "/Library", label: "Bibliotek", requiresAuth: true },
  { href: "/finance", label: "Finans", requiresAuth: true }
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
        <SidebarNav title={title} sections={sections} active={active} ariaLabel="Brugersektioner" />
        <section className={`menu-content ${contentClassName}`.trim()}>{children}</section>
      </section>
    </main>
  );
}
