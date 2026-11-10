using System;
using GamMaSite.Models;
using Xunit;

namespace GamMaSite.Tests;

public class EventNotificationSettingsPolicyTests
{
    [Fact]
    public void NullUsesDefaultPolicy()
    {
        AppliesExpectedNotificationPolicy(null!, true, true, true);
    }

    [Theory]
    [InlineData(EventNotificationSettingsPolicy.Default, true, true, true)]
    [InlineData(EventNotificationSettingsPolicy.OnlyWhenNoPaymentOffer, false, true, true)]
    [InlineData(EventNotificationSettingsPolicy.Never, false, false, false)]
    public void AppliesExpectedNotificationPolicy(
        string setting,
        bool sendsRegistrationConfirmation,
        bool sendsPaymentConfirmation,
        bool sendsWaitlistPromotion)
    {
        Assert.Equal(
            sendsRegistrationConfirmation,
            EventNotificationSettingsPolicy.ShouldSendRegistrationConfirmation(setting));
        Assert.Equal(
            sendsPaymentConfirmation,
            EventNotificationSettingsPolicy.ShouldSendPaymentConfirmation(PaymentOfferScopes.Event, setting));
        Assert.Equal(
            sendsWaitlistPromotion,
            EventNotificationSettingsPolicy.ShouldSendWaitlistPromotion(setting));
    }

    [Fact]
    public void KeepsMembershipPaymentEmailsIndependentOfEventSettings()
    {
        Assert.True(EventNotificationSettingsPolicy.ShouldSendPaymentConfirmation(
            PaymentOfferScopes.Membership,
            EventNotificationSettingsPolicy.Never));
    }

    [Fact]
    public void RejectsUnknownSettingWhenSaving()
    {
        Assert.Throws<ArgumentException>(() => EventNotificationSettingsPolicy.Normalize("SOMETIMES"));
    }
}
