import test from "node:test";
import assert from "node:assert/strict";
import { contactTypes, getContactTopicLabel } from "../../frontend/services/contactForm.js";
import { canDeleteEmailTemplate } from "../../frontend/services/emailTemplatePolicy.js";

test("contact form offers the three supported inquiry types", () => {
  assert.deepEqual(contactTypes, ["Arrangement", "Betaling og refusion", "Andet"]);
});

test("contact topic label follows the selected inquiry type", () => {
  assert.equal(getContactTopicLabel("Arrangement"), "Arrangementets titel");
  assert.equal(getContactTopicLabel("Betaling og refusion"), "Emne");
  assert.equal(getContactTopicLabel("Andet"), "Emne");
});

test("system email templates cannot be deleted from admin", () => {
  assert.equal(canDeleteEmailTemplate("SYSTEM_EVENT_CONFIRMATION"), false);
  assert.equal(canDeleteEmailTemplate("system_contact_request"), false);
  assert.equal(canDeleteEmailTemplate("EVENT"), true);
});
