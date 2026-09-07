const allowedTags = new Set([
  "P", "BR", "STRONG", "B", "EM", "I", "U", "S", "DEL", "H2", "H3", "UL", "OL", "LI",
  "A", "IMG", "FONT", "BLOCKQUOTE", "TABLE", "THEAD", "TBODY", "TR", "TH", "TD", "HR"
]);
const removedTags = new Set(["SCRIPT", "STYLE", "IFRAME", "OBJECT", "EMBED", "FORM", "VIDEO", "AUDIO"]);

/**
 * Sanitizes user-editable HTML before rendering it in React.
 */
export function sanitizeHtml(value = "") {
  value = typeof value === "string" ? value : String(value ?? "");
  if (typeof window === "undefined" || !value.includes("<")) {
    return value.replaceAll("\n", "<br>");
  }

  try {
    const documentFragment = new DOMParser().parseFromString(value, "text/html");
    sanitizeChildren(documentFragment.body);
    return documentFragment.body.innerHTML;
  } catch {
    return value.replaceAll("<", "&lt;").replaceAll(">", "&gt;");
  }
}

function sanitizeChildren(parent) {
  [...parent.children].forEach((element) => {
    if (removedTags.has(element.tagName)) {
      element.remove();
      return;
    }
    sanitizeChildren(element);
    if (!allowedTags.has(element.tagName)) {
      element.replaceWith(...[...element.childNodes]);
      return;
    }
    [...element.attributes].forEach((attribute) => {
      const name = attribute.name.toLowerCase();
      const isAllowed = element.tagName === "A" && ["href", "target", "rel"].includes(name)
        || element.tagName === "IMG" && ["src", "alt", "title", "width", "height", "style"].includes(name)
        || element.tagName === "FONT" && name === "face";
      if (!isAllowed) element.removeAttribute(attribute.name);
    });
    if (element.tagName === "IMG" && element.hasAttribute("style")) {
      const style = element.getAttribute("style") ?? "";
      const safeStyle = ["display:block", "margin-left:auto", "margin-left:0", "margin-right:auto", "margin-right:0"]
        .filter((rule) => style.replaceAll(" ", "").toLowerCase().includes(rule))
        .join(";");
      if (safeStyle) element.setAttribute("style", safeStyle);
      else element.removeAttribute("style");
    }
    if (element.tagName === "A" && !/^(https?:|mailto:|\/)/i.test(element.getAttribute("href") ?? "")) {
      element.removeAttribute("href");
    }
    if (element.tagName === "IMG" && !/^(https?:|\/)/i.test(element.getAttribute("src") ?? "")) {
      element.remove();
    }
  });
}

/**
 * Converts HTML content into plain text for summaries and generated files.
 */
export function htmlToText(value = "") {
  if (!value.includes("<")) return value;
  const documentFragment = new DOMParser().parseFromString(value, "text/html");
  return documentFragment.body.textContent ?? "";
}
