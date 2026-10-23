import { createContext, useContext } from "react";

export const permissions = {
  contentEdit: "content.edit",
  registrationsEdit: "registrations.edit",
  emailTemplatesEdit: "email_templates.edit",
  messagesEdit: "messages.edit",
  rolesEdit: "roles.edit",
  financeViewAll: "finance.view.all",
  financeEditAll: "finance.edit.all",
  paymentsEditAll: "payments.edit.all"
};

export const AccessContext = createContext({ permissions: [] });

const adminDestinations = [
  { path: "/admin/events", permission: permissions.contentEdit },
  { path: "/admin/products", permission: permissions.paymentsEditAll },
  { path: "/admin/subscriptions", permission: permissions.paymentsEditAll },
  { path: "/admin/messages", permission: permissions.messagesEdit },
  { path: "/admin/templates", permission: permissions.emailTemplatesEdit },
  { path: "/admin/users", permission: permissions.rolesEdit },
  { path: "/admin/finance", permission: permissions.financeViewAll }
];

export function useAccessUser() {
  return useContext(AccessContext);
}

export function hasPermission(user, permission) {
  const available = user?.permissions ?? [];
  return available.includes(permission)
    || (permission === permissions.financeViewAll && available.includes(permissions.financeEditAll));
}

export function hasAnyPermission(user, permissionList) {
  return permissionList.some((permission) => hasPermission(user, permission));
}

export function canAccessAdmin(user) {
  return adminDestinations.some((destination) => hasPermission(user, destination.permission));
}

export function firstAdminPath(user) {
  if (user?.isLoading) {
    return null;
  }

  return adminDestinations.find((destination) => hasPermission(user, destination.permission))?.path ?? "/events";
}

export function adminPermissionForPath(path) {
  if (path === "/admin/events" || path === "/admin/events/new") {
    return permissions.contentEdit;
  }
  if (path === "/admin/news" || path.startsWith("/admin/news/")) {
    return permissions.contentEdit;
  }
  if (path === "/admin/products" || path.startsWith("/admin/products/")) {
    return permissions.paymentsEditAll;
  }
  if (path === "/admin/subscriptions" || path.startsWith("/admin/subscriptions/")) {
    return permissions.paymentsEditAll;
  }
  if (path === "/admin/messages") {
    return permissions.messagesEdit;
  }
  if (path === "/admin/templates" || path.startsWith("/admin/templates/")) {
    return permissions.emailTemplatesEdit;
  }
  if (path === "/admin/users" || path.startsWith("/admin/users/") || path === "/admin/roles" || path.startsWith("/admin/roles/")) {
    return permissions.rolesEdit;
  }
  if (path === "/admin/finance/postings/edit") {
    return permissions.financeEditAll;
  }
  if (path === "/admin/finance" || path === "/admin/finance/postings" || path.startsWith("/admin/finance/postings/")) {
    return permissions.financeViewAll;
  }
  if (path.startsWith("/admin/finance/")) {
    return permissions.financeEditAll;
  }
  return null;
}
