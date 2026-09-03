export const adminItems = [
  { href: "/react/admin/events", label: "Events", readAdmin: true },
  { href: "/react/admin/news", label: "Nyheder" },
  { href: "/react/admin/users", label: "Medlemmer" },
  { href: "/react/admin/messages", label: "Beskeder" },
  { href: "/react/admin/roles", label: "Roller" },
  { href: "/react/admin/templates", label: "Besked Skabelon" }
];

export function navigate(href) {
  window.history.pushState({}, "", href);
  window.dispatchEvent(new Event("gammasite:navigate"));
}

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
