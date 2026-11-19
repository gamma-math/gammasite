export const contactTypes = ["Arrangement", "Betaling og refusion", "Andet"];

export function getContactTopicLabel(contactType) {
  return contactType === "Arrangement" ? "Arrangementets titel" : "Emne";
}
