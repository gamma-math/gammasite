import { useEffect, useState } from "react";
import { Menu, X } from "lucide-react";
import { Link } from "../routes/navigation.jsx";
import { mobileMenuStateEvent, readMobileMenuState, setMobileMenuCollapsed } from "../utils/mobileMenu.js";

const mobileBreakpoint = "(max-width: 640px)";

function isMobileNavigation() {
  return typeof window !== "undefined"
    && typeof window.matchMedia === "function"
    && window.matchMedia(mobileBreakpoint).matches;
}

/**
 * Shared sidebar navigation used by member areas and public information pages.
 */
export function SidebarNav({ title, sections, active, ariaLabel = "Navigation", renderLink }) {
  const [isCollapsed, setIsCollapsed] = useState(readMobileMenuState);

  useEffect(() => {
    const syncMenuState = () => setIsCollapsed(readMobileMenuState());
    window.addEventListener(mobileMenuStateEvent, syncMenuState);
    return () => window.removeEventListener(mobileMenuStateEvent, syncMenuState);
  }, []);

  function handleNavigation() {
    if (!isMobileNavigation()) {
      return;
    }

    setMobileMenuCollapsed(true);
    setIsCollapsed(true);
  }

  function toggleMenu() {
    setIsCollapsed((current) => {
      const next = !current;
      setMobileMenuCollapsed(next);
      return next;
    });
  }

  return (
    <aside className={`menu-sidebar ${isCollapsed ? "is-collapsed" : ""}`.trim()}>
      <div className="menu-sidebar-header">
        <h2 className="menu-sidebar-title">{title}</h2>
        <button
          className="menu-sidebar-toggle"
          type="button"
          aria-expanded={!isCollapsed}
          aria-controls="menu-sidebar-navigation"
          aria-label={isCollapsed ? "Åbn menu" : "Luk menu"}
          onClick={toggleMenu}
        >
          {isCollapsed ? <Menu size={20} aria-hidden="true" /> : <X size={20} aria-hidden="true" />}
        </button>
      </div>
      <nav id="menu-sidebar-navigation" className="menu-side-nav" aria-label={ariaLabel}>
        {sections.map((section, index) => (
          <div className="menu-nav-section" key={section.label ?? `section-${index}`}>
            {section.label && <p className="menu-nav-section-title">{section.label}</p>}
            {section.items.map((item) => {
              const isActive = active === item.href || active === item.id;
              const className = `menu-side-link ${isActive ? "is-active" : ""}`.trim();

              return renderLink
                ? renderLink(item, className, isActive, handleNavigation)
                : (
                  <Link className={className} href={item.href} key={item.href} onClick={handleNavigation}>
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
