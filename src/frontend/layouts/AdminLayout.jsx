import { adminSections, Link } from "../routes/navigation.jsx";
import { CircleArrowLeft } from "lucide-react";
import { hasPermission, useAccessUser } from "../utils/access.js";

const financeSections = [
  {
    items: [
      {
        href: "/react/admin/events",
        label: "Tilbage til Admin",
        icon: CircleArrowLeft,
      },
      { href: "/react/admin/finance", label: "Overblik", permission: "finance.view.all" },
      { href: "/react/admin/finance/postings", label: "Posteringer", permission: "finance.view.all" },
    ],
  },
  {
    label: "Rediger finanser",
    items: [
      { href: "/react/admin/finance/csv-import", label: "CSV-import", permission: "finance.edit.all" },
      {
        href: "/react/admin/finance/postings/edit",
        label: "Posteringer",
        permission: "finance.edit.all",
      },
      { href: "/react/admin/finance/chart-of-accounts", label: "Kontoplan", permission: "finance.edit.all" },
      {
        href: "/react/admin/finance/posting-groups",
        label: "Posteringsgrupper",
        permission: "finance.edit.all",
      },
      { href: "/react/admin/finance/budgets", label: "Budgetter", permission: "finance.edit.all" },
    ],
  },
];

function financeActivePath(pathname) {
  if (pathname === "/react/admin/finance") return "/react/admin/finance";
  if (pathname === "/react/admin/finance/postings") {
    return "/react/admin/finance/postings";
  }
  if (pathname.startsWith("/react/admin/finance/postings/")) {
    return "/react/admin/finance/postings/edit";
  }
  if (pathname.startsWith("/react/admin/finance/chart-of-accounts")) {
    return "/react/admin/finance/chart-of-accounts";
  }
  if (pathname.startsWith("/react/admin/finance/posting-groups")) {
    return "/react/admin/finance/posting-groups";
  }
  if (pathname.startsWith("/react/admin/finance/budgets")) {
    return "/react/admin/finance/budgets";
  }
  if (pathname.startsWith("/react/admin/finance/csv-import")) {
    return "/react/admin/finance/csv-import";
  }
  return "/react/admin/finance";
}

/**
 * Shared admin layout that hides write-only sections from read-only admins.
 */
export function AdminLayout({
  active,
  canWrite,
  children,
  contentClassName = "",
}) {
  const accessUser = useAccessUser();
  const isFinanceAdmin = window.location.pathname.startsWith(
    "/react/admin/finance",
  );
  const sections = isFinanceAdmin
    ? financeSections
        .map((section) => ({
          ...section,
          items: section.items.filter((item) => !item.permission || hasPermission(accessUser, item.permission)),
        }))
        .filter((section) => section.items.length > 0)
    : adminSections
        .map((section) => ({
          ...section,
          items: section.items.filter((item) => !item.permission || hasPermission(accessUser, item.permission)),
        }))
        .filter((section) => section.items.length > 0);
  const activePath = isFinanceAdmin
    ? financeActivePath(window.location.pathname)
    : active;

  return (
    <main className="menu-shell">
      <section className="menu-workspace">
        <aside className="menu-sidebar">
          <h2 className="menu-sidebar-title">
            {isFinanceAdmin ? "Finans Admin" : "Admin"}
          </h2>
          <nav className="menu-side-nav" aria-label="Admin sektioner">
            {sections.map((section, index) => (
              <div
                className="menu-nav-section"
                key={section.label ?? `section-${index}`}
              >
                {section.label && (
                  <p className="menu-nav-section-title">{section.label}</p>
                )}
                {section.items.map((item) => {
                  const Icon = item.icon;
                  return (
                    <Link
                      className={`menu-side-link ${activePath === item.href ? "is-active" : ""}`}
                      href={item.href}
                      key={item.href}
                    >
                      {Icon && <Icon size={20} strokeWidth={2.2} aria-hidden="true" />}
                      {item.label}
                    </Link>
                  );
                })}
              </div>
            ))}
          </nav>
        </aside>
        <section className={`menu-content ${contentClassName}`.trim()}>
          {children}
        </section>
      </section>
    </main>
  );
}
