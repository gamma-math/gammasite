export function currentReturnUrl(location = window.location) {
  return (location.pathname + location.search + location.hash) || "/react";
}

export function loginHref(returnUrl, location = window.location) {
  return authHref("/react/account/login", returnUrl, location);
}

export function authHref(path, returnUrl, location = window.location) {
  const target = returnUrl || currentReturnUrl(location);
  return path + "?returnUrl=" + encodeURIComponent(target);
}
