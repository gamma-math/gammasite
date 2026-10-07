import { createContext, useContext } from "react";

export const permissions = {
  contentEdit: "content.edit",
  registrationsEdit: "registrations.edit",
  emailTemplatesEdit: "email_templates.edit",
  messagesEdit: "messages.edit",
  rolesEdit: "roles.edit",
  financeViewAll: "finance.view.all",
  financeEditAll: "finance.edit.all"
};

export const AccessContext = createContext({ permissions: [] });

const adminDestinations = [
  { path: "/react/admin/events", permission: permissions.contentEdit },
  { path: "/react/admin/messages", permission: permissions.messagesEdit },
  { path: "/react/admin/templates", permission: permissions.emailTemplatesEdit },
  { path: "/react/admin/users", permission: permissions.rolesEdit },
  { path: "/react/admin/finance", permission: permissions.financeViewAll }
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

  return adminDestinations.find((destination) => hasPermission(user, destination.permission))?.path ?? "/react/events";
}

export function adminPermissionForPath(path) {
  if (path === "/react/admin/events" || path === "/react/admin/events/new") {
    return permissions.contentEdit;
  }
  if (path === "/react/admin/news" || path.startsWith("/react/admin/news/")) {
    return permissions.contentEdit;
  }
  if (path === "/react/admin/messages") {
    return permissions.messagesEdit;
  }
  if (path === "/react/admin/templates" || path.startsWith("/react/admin/templates/")) {
    return permissions.emailTemplatesEdit;
  }
  if (path === "/react/admin/users" || path === "/react/admin/roles" || path.startsWith("/react/admin/roles/")) {
    return permissions.rolesEdit;
  }
  if (path === "/react/admin/finance/postings/edit") {
    return permissions.financeEditAll;
  }
  if (path === "/react/admin/finance" || path === "/react/admin/finance/postings" || path.startsWith("/react/admin/finance/postings/")) {
    return permissions.financeViewAll;
  }
  if (path.startsWith("/react/admin/finance/")) {
    return permissions.financeEditAll;
  }
  return null;
}
