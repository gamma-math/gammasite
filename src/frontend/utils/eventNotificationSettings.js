export const EVENT_NOTIFICATION_SETTINGS = Object.freeze({
  DEFAULT: "DEFAULT",
  ONLY_WHEN_NO_PAYMENT_OFFER: "OnlyWhenNoPaymentOffer",
  NEVER: "NEVER"
});

export const EVENT_NOTIFICATION_SETTING_OPTIONS = Object.freeze([
  {
    value: EVENT_NOTIFICATION_SETTINGS.DEFAULT,
    label: "Standard"
  },
  {
    value: EVENT_NOTIFICATION_SETTINGS.ONLY_WHEN_NO_PAYMENT_OFFER,
    label: "Kun betaling og venteliste"
  },
  {
    value: EVENT_NOTIFICATION_SETTINGS.NEVER,
    label: "Send ingen mails"
  }
]);

export function normalizeEventNotificationSettings(value) {
  return EVENT_NOTIFICATION_SETTING_OPTIONS.some((option) => option.value === value)
    ? value
    : EVENT_NOTIFICATION_SETTINGS.DEFAULT;
}
