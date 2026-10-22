export const mobileMenuStorageKey = "gammasite.mobile-menu-collapsed";
export const mobileMenuStateEvent = "gammasite:mobile-menu-state";

export function readMobileMenuState() {
  if (typeof window === "undefined" || !window.sessionStorage) {
    return false;
  }

  return window.sessionStorage.getItem(mobileMenuStorageKey) === "true";
}

export function setMobileMenuCollapsed(isCollapsed) {
  if (typeof window !== "undefined" && window.sessionStorage) {
    window.sessionStorage.setItem(mobileMenuStorageKey, String(isCollapsed));
    window.dispatchEvent(new Event(mobileMenuStateEvent));
  }
}
