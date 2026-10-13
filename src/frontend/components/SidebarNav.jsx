import { Link } from "../routes/navigation.jsx";

/**
 * Shared sidebar navigation used by member areas and public information pages.
 */
export function SidebarNav({ title, sections, active, ariaLabel = "Navigation", renderLink }) {
  return (
    <aside className="menu-sidebar">
      <h2 className="menu-sidebar-title">{title}</h2>
      <nav className="menu-side-nav" aria-label={ariaLabel}>
        {sections.map((section, index) => (
          <div className="menu-nav-section" key={section.label ?? `section-${index}`}>
            {section.label && <p className="menu-nav-section-title">{section.label}</p>}
            {section.items.map((item) => {
              const isActive = active === item.href || active === item.id;
              const className = `menu-side-link ${isActive ? "is-active" : ""}`.trim();

              return renderLink
                ? renderLink(item, className, isActive)
                : (
                  <Link className={className} href={item.href} key={item.href}>
                    {item.label}
                  </Link>
                );
            })}
          </div>
        ))}
      </nav>
    </aside>
  );
}
