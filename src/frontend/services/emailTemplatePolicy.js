export function isSystemTemplate(templateType) {
  return String(templateType ?? "").toUpperCase().startsWith("SYSTEM_");
}

export function canDeleteEmailTemplate(templateType) {
  return !isSystemTemplate(templateType);
}
