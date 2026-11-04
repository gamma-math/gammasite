using Stripe.Checkout;
using Xunit;
using GamMaSite.Services;

namespace GamMaSite.Tests;

/*
 * Tests the local payment-state rules in StripeService.
 * External Stripe API calls are intentionally not executed in unit tests.
 */
public class StripeServiceTests
{
    [Theory]
    [InlineData("charge", "charge", true)]
    [InlineData("payment", "payment", true)]
    [InlineData("stripe_fee", "stripe_fee", false)]
    [InlineData("refund", "refund", false)]
    [InlineData("charge", "stripe_fee", false)]
    [InlineData("charge", "", true)]
    public void IsPaymentTransaction_OnlyAcceptsPaymentCategories(
        string transactionType,
        string reportingCategory,
        bool expected)
    {
        Assert.Equal(
            expected,
            StripeService.IsPaymentTransaction(transactionType, reportingCategory));
    }

    [Theory]
    [InlineData("paid", true)]
    [InlineData("no_payment_required", true)]
    [InlineData("unpaid", false)]
    [InlineData("", false)]
    public void IsPaymentComplete_RecognizesStripePaymentStates(string status, bool expected)
    {
        var session = new Session { PaymentStatus = status };

        Assert.Equal(expected, new StripeService().IsPaymentComplete(session));
    }
}
