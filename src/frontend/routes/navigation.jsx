import { authHref } from "../utils/authNavigation.js";

export const adminSections = [
  {
    items: [
      { href: "/admin/events", label: "Events", permission: "content.edit" },
      { href: "/admin/news", label: "Nyheder", permission: "content.edit" },
    ],
  },
  {
    label: "Betalingsaftaler",
    items: [
      { href: "/admin/products", label: "Produkter", permission: "payments.edit.all" },
      { href: "/admin/subscriptions", label: "Betalingsaftaler", permission: "payments.edit.all" },
    ],
  },
  {
    label: "Beskeder",
    items: [
      { href: "/admin/messages", label: "Beskeder", permission: "messages.edit" },
      { href: "/admin/templates", label: "Besked Skabeloner", permission: "email_templates.edit" },
    ],
  },
  {
    label: "Medlemmer",
    items: [
      { href: "/admin/users", label: "Medlemmer", permission: "roles.edit" },
      { href: "/admin/roles", label: "Roller", permission: "roles.edit" },
    ],
  },
  {
    label: "Finans",
    items: [{ href: "/admin/finance", label: "Finans", permission: "finance.view.all" }],
  },
];

// Kept as a flat export for consumers that only need the complete item list.
export const adminItems = adminSections.flatMap((section) => section.items);

/**
 * Updates the URL and notifies the app router without a full page reload.
 */
export function navigate(href) {
  window.history.pushState({}, "", href);
  window.dispatchEvent(new Event("gammasite:navigate"));
}

/**
 * Internal link component for React routes with normal anchor fallback behavior.
 */
export function Link({ href, className, children, onClick, ...props }) {
  const targetHref = withAuthReturnUrl(href);

  return (
    <a
      href={targetHref}
      className={className}
      onClick={(event) => {
        if (targetHref.startsWith("/")) {
          event.preventDefault();
          navigate(targetHref);
        }
        onClick?.(event);
      }}
      {...props}
    >
      {children}
    </a>
  );
}

function withAuthReturnUrl(href) {
  if (href !== "/account/login" && href !== "/account/register") {
    return href;
  }

  const location = window.location;
  const currentParams = new URLSearchParams(location.search);
  const existingReturnUrl = currentParams.get("returnUrl") ?? currentParams.get("ReturnUrl");
  const isAuthPage = location.pathname === "/account/login" || location.pathname === "/account/register";
  return authHref(href, isAuthPage && existingReturnUrl ? existingReturnUrl : undefined, location);
}
