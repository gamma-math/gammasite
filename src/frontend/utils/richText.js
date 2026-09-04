const allowedTags = new Set(["P", "BR", "STRONG", "B", "EM", "I", "U", "H2", "H3", "UL", "OL", "LI", "A", "BLOCKQUOTE"]);
const removedTags = new Set(["SCRIPT", "STYLE", "IFRAME", "OBJECT", "EMBED", "FORM"]);

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
      if (attribute.name !== "href" || element.tagName !== "A") element.removeAttribute(attribute.name);
    });
    if (element.tagName === "A" && !/^(https?:|mailto:)/i.test(element.getAttribute("href") ?? "")) {
      element.removeAttribute("href");
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
