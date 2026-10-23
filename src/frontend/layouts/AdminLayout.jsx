import { adminSections, Link } from "../routes/navigation.jsx";
import { CircleArrowLeft } from "lucide-react";
import { SidebarNav } from "../components/SidebarNav.jsx";
import { hasPermission, useAccessUser } from "../utils/access.js";

const financeSections = [
  {
    items: [
      {
        href: "/admin/events",
        label: "Tilbage til Admin",
        icon: CircleArrowLeft,
        permissions: ["content.edit", "roles.edit", "email_templates.edit", "messages.edit"],
      },
      { href: "/admin/finance", label: "Overblik", permission: "finance.view.all" },
      { href: "/admin/finance/postings", label: "Posteringer", permission: "finance.view.all" },
    ],
  },
  {
    label: "Rediger finanser",
    items: [
      { href: "/admin/finance/csv-import", label: "CSV-import", permission: "finance.edit.all" },
      {
        href: "/admin/finance/postings/edit",
        label: "Posteringer",
        permission: "finance.edit.all",
      },
      { href: "/admin/finance/chart-of-accounts", label: "Kontoplan", permission: "finance.edit.all" },
      {
        href: "/admin/finance/posting-groups",
        label: "Posteringsgrupper",
        permission: "finance.edit.all",
      },
      { href: "/admin/finance/budgets", label: "Budgetter", permission: "finance.edit.all" },
    ],
  },
];

function financeActivePath(pathname) {
  if (pathname === "/admin/finance") return "/admin/finance";
  if (pathname === "/admin/finance/postings") {
    return "/admin/finance/postings";
  }
  if (pathname.startsWith("/admin/finance/postings/")) {
    return "/admin/finance/postings/edit";
  }
  if (pathname.startsWith("/admin/finance/chart-of-accounts")) {
    return "/admin/finance/chart-of-accounts";
  }
  if (pathname.startsWith("/admin/finance/posting-groups")) {
    return "/admin/finance/posting-groups";
  }
  if (pathname.startsWith("/admin/finance/budgets")) {
    return "/admin/finance/budgets";
  }
  if (pathname.startsWith("/admin/finance/csv-import")) {
    return "/admin/finance/csv-import";
  }
  return "/admin/finance";
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
    "/admin/finance",
  );
  const sections = isFinanceAdmin
    ? financeSections
        .map((section) => ({
          ...section,
          items: section.items.filter((item) =>
            !item.permission && !item.permissions
              ? true
              : item.permission
                ? hasPermission(accessUser, item.permission)
                : item.permissions.some((permission) => hasPermission(accessUser, permission)),
          ),
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
        <SidebarNav
          title={isFinanceAdmin ? "Finans Admin" : "Admin"}
          sections={sections}
          active={activePath}
          ariaLabel="Admin sektioner"
          renderLink={(item, className, _isActive, onNavigate) => {
            const Icon = item.icon;
            return (
              <Link className={className} href={item.href} key={item.href} onClick={onNavigate}>
                {Icon && <Icon size={20} strokeWidth={2.2} aria-hidden="true" />}
                {item.label}
              </Link>
            );
          }}
        />
        <section className={`menu-content ${contentClassName}`.trim()}>
          {children}
        </section>
      </section>
    </main>
  );
}
