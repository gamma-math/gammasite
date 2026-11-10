import test from "node:test";
import assert from "node:assert/strict";
import {
  EVENT_NOTIFICATION_SETTINGS,
  EVENT_NOTIFICATION_SETTING_OPTIONS,
  normalizeEventNotificationSettings
} from "../../frontend/utils/eventNotificationSettings.js";

test("exposes the three event notification choices in Danish", () => {
  assert.deepEqual(
    EVENT_NOTIFICATION_SETTING_OPTIONS.map((option) => option.value),
    [
      EVENT_NOTIFICATION_SETTINGS.DEFAULT,
      EVENT_NOTIFICATION_SETTINGS.ONLY_WHEN_NO_PAYMENT_OFFER,
      EVENT_NOTIFICATION_SETTINGS.NEVER
    ]
  );
  assert.ok(EVENT_NOTIFICATION_SETTING_OPTIONS.every((option) => option.label.length > 0));
});

test("normalizes null and unknown event settings to the default choice", () => {
  assert.equal(normalizeEventNotificationSettings(null), EVENT_NOTIFICATION_SETTINGS.DEFAULT);
  assert.equal(normalizeEventNotificationSettings("UNKNOWN"), EVENT_NOTIFICATION_SETTINGS.DEFAULT);
  assert.equal(
    normalizeEventNotificationSettings(EVENT_NOTIFICATION_SETTINGS.NEVER),
    EVENT_NOTIFICATION_SETTINGS.NEVER
  );
});
