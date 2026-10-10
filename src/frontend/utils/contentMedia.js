export const MAX_CONTENT_IMAGE_SIZE = 2 * 1024 * 1024;

export function isSupportedContentImage(file) {
  if (!file) return false;
  const name = String(file.name ?? "").toLowerCase();
  return file.size <= MAX_CONTENT_IMAGE_SIZE && [".png", ".jpg", ".jpeg"].some((extension) => name.endsWith(extension));
}

export function isLocalContentImage(url) {
  return typeof url === "string" && url.startsWith("/media/content/");
}
