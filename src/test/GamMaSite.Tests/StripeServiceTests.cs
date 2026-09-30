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
