export const adminSections = [
  {
    items: [
      { href: "/react/admin/events", label: "Events", readAdmin: true },
      { href: "/react/admin/news", label: "Nyheder" },
    ],
  },
  {
    label: "Beskeder",
    items: [
      { href: "/react/admin/messages", label: "Beskeder" },
      { href: "/react/admin/templates", label: "Besked Skabeloner" },
    ],
  },
  {
    label: "Medlemmer",
    items: [
      { href: "/react/admin/users", label: "Medlemmer" },
      { href: "/react/admin/roles", label: "Roller" },
    ],
  },
  {
    label: "Finans",
    items: [{ href: "/react/admin/finance", label: "Finans" }],
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
export function Link({ href, className, children, ...props }) {
  return (
    <a
      href={href}
      className={className}
      onClick={(event) => {
        if (href.startsWith("/react")) {
          event.preventDefault();
          navigate(href);
        }
      }}
      {...props}
    >
      {children}
    </a>
  );
}
