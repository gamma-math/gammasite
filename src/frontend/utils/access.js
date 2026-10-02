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
  return hasAnyPermission(user, Object.values(permissions));
}
