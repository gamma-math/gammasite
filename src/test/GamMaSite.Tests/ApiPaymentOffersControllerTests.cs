using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Stripe.Checkout;
using Xunit;

namespace GamMaSite.Tests;

public sealed class ApiPaymentOffersControllerTests
{
    [Fact]
    public async Task Delete_RejectsUserWithoutEventEditAccess()
    {
        await using var db = CreateDatabase();
        var offer = await SeedOfferAsync(db);
        var access = new Mock<IAccessControlService>();
        var contentItemId = Assert.IsType<int>(offer.ContentItemId);
        access.Setup(value => value.CanEditEventAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>(), contentItemId))
            .ReturnsAsync(false);

        var stripe = new Mock<IStripeService>();
        var controller = new ApiPaymentOffersController(db, stripe.Object, access.Object);

        var result = await controller.Delete(offer.Id);

        Assert.IsType<ForbidResult>(result);
        Assert.NotNull(await db.PaymentOffers.FindAsync(offer.Id));
        stripe.Verify(value => value.DeactivatePriceAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Delete_RemovesOfferAndLegacyPaymentLinkButPreservesManualPaymentLink()
    {
        await using var db = CreateDatabase();
        var offer = await SeedOfferAsync(db);
        var access = new Mock<IAccessControlService>();
        var contentItemId = Assert.IsType<int>(offer.ContentItemId);
        access.Setup(value => value.CanEditEventAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>(), contentItemId))
            .ReturnsAsync(true);

        var stripe = new Mock<IStripeService>();
        stripe.Setup(value => value.DeactivatePriceAsync("price_event"))
            .ReturnsAsync(true);
        var controller = new ApiPaymentOffersController(db, stripe.Object, access.Object);

        var result = await controller.Delete(offer.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await db.PaymentOffers.FindAsync(offer.Id));
        var remainingLinks = await db.ContentLinks
            .Where(link => link.ContentItemId == offer.ContentItemId)
            .ToListAsync();
        var remainingLink = Assert.Single(remainingLinks);
        Assert.Equal("https://tickets.example.com/event", remainingLink.Url);
        Assert.Equal("PAYMENT", remainingLink.Type);
        stripe.Verify(value => value.DeactivatePriceAsync("price_event"), Times.Once);
    }

    [Fact]
    public async Task SaveForEventStoresPaymentOfferLabelWithoutCreatingContentLink()
    {
        await using var db = CreateDatabase();
        var item = new ContentItem
        {
            Type = ContentTypes.Event,
            Status = ContentStatuses.Published,
            Title = "Test event",
            Slug = "test-event",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow,
            Links =
            {
                new ContentLink
                {
                    Label = "Ekstern betaling",
                    Url = "https://tickets.example.com/event",
                    Type = "PAYMENT",
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow
                }
            }
        };
        db.ContentItems.Add(item);
        await db.SaveChangesAsync();

        var access = new Mock<IAccessControlService>();
        access.Setup(value => value.CanEditEventAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>(), item.Id))
            .ReturnsAsync(true);
        var stripe = new Mock<IStripeService>();
        var controller = new ApiPaymentOffersController(db, stripe.Object, access.Object);

        var result = await controller.SaveForEvent(item.Id, new SavePaymentOfferRequest
        {
            LinkLabel = "Køb billet",
            Provider = PaymentOfferProviders.MobilePay,
            Amount = 125,
            ProductName = "Billet"
        });

        var response = Assert.IsType<OkObjectResult>(result);
        var offer = Assert.IsType<PaymentOfferDto>(response.Value);
        Assert.Equal("Køb billet", offer.LinkLabel);
        Assert.Equal("/react/events/test-event/pay/1", offer.PaymentUrl);
        Assert.Single(await db.PaymentOffers.Where(value => value.ContentItemId == item.Id).ToListAsync());
        var links = await db.ContentLinks.Where(link => link.ContentItemId == item.Id).ToListAsync();
        var manualLink = Assert.Single(links);
        Assert.Equal("https://tickets.example.com/event", manualLink.Url);
    }

    [Fact]
    public async Task SaveForEventRejectsPaymentOfferBelowMinimumAmount()
    {
        await using var db = CreateDatabase();
        var item = new ContentItem
        {
            Type = ContentTypes.Event,
            Status = ContentStatuses.Published,
            Title = "Test event",
            Slug = "test-event",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.ContentItems.Add(item);
        await db.SaveChangesAsync();

        var access = new Mock<IAccessControlService>();
        access.Setup(value => value.CanEditEventAsync(It.IsAny<ClaimsPrincipal>(), item.Id))
            .ReturnsAsync(true);
        var controller = new ApiPaymentOffersController(
            db,
            new Mock<IStripeService>().Object,
            access.Object);

        var result = await controller.SaveForEvent(item.Id, new SavePaymentOfferRequest
        {
            LinkLabel = "Køb billet",
            Provider = PaymentOfferProviders.Stripe,
            Amount = 9.99m,
            ProductName = "Billet"
        });

        var response = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("10,00", response.Value?.ToString());
        Assert.Empty(await db.PaymentOffers.ToListAsync());
    }

    [Fact]
    public async Task GetForAdminReturnsEventAndMembershipOffers()
    {
        await using var db = CreateDatabase();
        var eventItem = new ContentItem
        {
            Type = ContentTypes.Event,
            Status = ContentStatuses.Published,
            Title = "Test event",
            Slug = "test-event",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.PaymentOffers.AddRange(
            new PaymentOffer
            {
                ContentItem = eventItem,
                Scope = PaymentOfferScopes.Event,
                Provider = PaymentOfferProviders.Stripe,
                OfferType = PaymentOfferTypes.OneTime,
                LinkLabel = "Køb billet",
                ProductName = "Billet",
                AmountMinor = 2500,
                Currency = "DKK",
                IsActive = true,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            },
            new PaymentOffer
            {
                Scope = PaymentOfferScopes.Membership,
                Provider = PaymentOfferProviders.MobilePay,
                OfferType = PaymentOfferTypes.Recurring,
                LinkLabel = "Betal automatisk",
                ProductName = "Kontingent",
                AmountMinor = 15000,
                Currency = "DKK",
                IsActive = true,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var controller = new ApiPaymentOffersController(
            db,
            new Mock<IStripeService>().Object,
            new Mock<IAccessControlService>().Object);

        var result = Assert.IsType<OkObjectResult>(await controller.GetForAdmin());
        var offers = Assert.IsAssignableFrom<System.Collections.IEnumerable>(result.Value)
            .Cast<PaymentOfferDto>()
            .ToList();

        Assert.Equal(2, offers.Count);
        Assert.Contains(offers, offer => offer.Scope == PaymentOfferScopes.Event && offer.EventTitle == "Test event");
        Assert.Contains(offers, offer => offer.Scope == PaymentOfferScopes.Membership && offer.EventTitle == null);
    }

    [Fact]
    public async Task GetEventOptionsForAdminReturnsEventsForProductFilter()
    {
        await using var db = CreateDatabase();
        db.ContentItems.AddRange(
            new ContentItem
            {
                Type = ContentTypes.Event,
                Status = ContentStatuses.Published,
                Title = "B Event",
                Slug = "b-event",
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            },
            new ContentItem
            {
                Type = ContentTypes.Event,
                Status = ContentStatuses.Draft,
                Title = "A Event",
                Slug = "a-event",
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            },
            new ContentItem
            {
                Type = ContentTypes.News,
                Status = ContentStatuses.Published,
                Title = "A News",
                Slug = "a-news",
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var controller = new ApiPaymentOffersController(
            db,
            new Mock<IStripeService>().Object,
            new Mock<IAccessControlService>().Object);

        var result = Assert.IsType<OkObjectResult>(await controller.GetEventOptionsForAdmin());
        var events = Assert.IsAssignableFrom<System.Collections.IEnumerable>(result.Value)
            .Cast<object>()
            .ToList();

        Assert.Equal(2, events.Count);
        Assert.Equal("A Event", events[0].GetType().GetProperty("title")?.GetValue(events[0]));
        Assert.Equal("B Event", events[1].GetType().GetProperty("title")?.GetValue(events[1]));
    }

    [Fact]
    public async Task UpdateForAdminUpdatesProductSettings()
    {
        await using var db = CreateDatabase();
        var offer = new PaymentOffer
        {
            Scope = PaymentOfferScopes.Membership,
            Provider = PaymentOfferProviders.MobilePay,
            OfferType = PaymentOfferTypes.Recurring,
            LinkLabel = "Gammelt label",
            ProductName = "Kontingent",
            AmountMinor = 15000,
            Currency = "DKK",
            BillingInterval = "YEAR",
            BillingIntervalCount = 1,
            IsActive = true,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.PaymentOffers.Add(offer);
        await db.SaveChangesAsync();

        var controller = new ApiPaymentOffersController(
            db,
            new Mock<IStripeService>().Object,
            new Mock<IAccessControlService>().Object);

        var result = Assert.IsType<OkObjectResult>(await controller.UpdateForAdmin(offer.Id, new UpdatePaymentOfferRequest
        {
            LinkLabel = "Nyt label",
            ProductName = "Nyt kontingent",
            Amount = 175,
            BillingInterval = "YEAR",
            BillingIntervalCount = 1,
            IsActive = false
        }));

        var updated = Assert.IsType<PaymentOfferDto>(result.Value);
        Assert.Equal("Nyt label", updated.LinkLabel);
        Assert.Equal("Nyt kontingent", updated.ProductName);
        Assert.Equal(17500, updated.AmountMinor);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task UpdateForAdminRejectsPriceBelowMinimumAmount()
    {
        await using var db = CreateDatabase();
        var offer = new PaymentOffer
        {
            Scope = PaymentOfferScopes.Membership,
            Provider = PaymentOfferProviders.MobilePay,
            OfferType = PaymentOfferTypes.Recurring,
            LinkLabel = "Betal automatisk",
            ProductName = "Kontingent",
            AmountMinor = 15000,
            Currency = "DKK",
            BillingInterval = "YEAR",
            BillingIntervalCount = 1,
            IsActive = true,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.PaymentOffers.Add(offer);
        await db.SaveChangesAsync();

        var controller = new ApiPaymentOffersController(
            db,
            new Mock<IStripeService>().Object,
            new Mock<IAccessControlService>().Object);

        var result = await controller.UpdateForAdmin(offer.Id, new UpdatePaymentOfferRequest
        {
            LinkLabel = offer.LinkLabel,
            ProductName = offer.ProductName,
            Amount = 9.99m,
            BillingInterval = offer.BillingInterval,
            BillingIntervalCount = offer.BillingIntervalCount
        });

        var response = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("10,00", response.Value?.ToString());
        Assert.Equal(15000, (await db.PaymentOffers.SingleAsync()).AmountMinor);
    }

    [Fact]
    public async Task UpdateForAdminUpdatesOpenMobilePayAgreementsWhenRecurringAmountChanges()
    {
        await using var db = CreateDatabase();
        var offer = new PaymentOffer
        {
            Scope = PaymentOfferScopes.Membership,
            Provider = PaymentOfferProviders.MobilePay,
            OfferType = PaymentOfferTypes.Recurring,
            LinkLabel = "Betal automatisk",
            ProductName = "Kontingent",
            AmountMinor = 15000,
            Currency = "DKK",
            BillingInterval = "YEAR",
            BillingIntervalCount = 1,
            IsActive = true,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.PaymentOffers.Add(offer);
        await db.SaveChangesAsync();

        db.PaymentSubscriptions.AddRange(
            new PaymentSubscription
            {
                UserId = "user-1",
                PaymentOfferId = offer.Id,
                Provider = PaymentOfferProviders.MobilePay,
                ProviderSubscriptionId = "active-agreement",
                ProviderProductId = offer.Id.ToString(),
                Status = PaymentSubscriptionStatuses.Active,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            },
            new PaymentSubscription
            {
                UserId = "user-2",
                PaymentOfferId = offer.Id,
                Provider = PaymentOfferProviders.MobilePay,
                ProviderSubscriptionId = "pending-agreement",
                ProviderProductId = offer.Id.ToString(),
                Status = PaymentSubscriptionStatuses.Pending,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            },
            new PaymentSubscription
            {
                UserId = "user-3",
                PaymentOfferId = offer.Id,
                Provider = PaymentOfferProviders.MobilePay,
                ProviderSubscriptionId = "stopped-agreement",
                ProviderProductId = offer.Id.ToString(),
                Status = PaymentSubscriptionStatuses.Canceled,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var mobilePay = new Mock<IMobilePayService>();
        mobilePay.Setup(value => value.UpdateAgreementAmountAsync(
                It.IsAny<string>(),
                17500,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new ApiPaymentOffersController(
            db,
            new Mock<IStripeService>().Object,
            new Mock<IAccessControlService>().Object,
            mobilePay.Object);

        var result = Assert.IsType<OkObjectResult>(await controller.UpdateForAdmin(offer.Id, new UpdatePaymentOfferRequest
        {
            LinkLabel = "Betal automatisk",
            ProductName = "Kontingent",
            Amount = 175,
            BillingInterval = "YEAR",
            BillingIntervalCount = 1,
            IsActive = true
        }));

        Assert.IsType<PaymentOfferDto>(result.Value);
        mobilePay.Verify(value => value.UpdateAgreementAmountAsync(
            "active-agreement",
            17500,
            It.IsAny<CancellationToken>()), Times.Once);
        mobilePay.Verify(value => value.UpdateAgreementAmountAsync(
            "pending-agreement",
            17500,
            It.IsAny<CancellationToken>()), Times.Once);
        mobilePay.Verify(value => value.UpdateAgreementAmountAsync(
            "stopped-agreement",
            It.IsAny<long>(),
            It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(17500, (await db.PaymentOffers.SingleAsync()).AmountMinor);
    }

    [Fact]
    public async Task UpdateForAdminCreatesNewStripePriceAndUpdatesOpenSubscriptionsWhenRecurringAmountChanges()
    {
        await using var db = CreateDatabase();
        var offer = new PaymentOffer
        {
            Scope = PaymentOfferScopes.Membership,
            Provider = PaymentOfferProviders.Stripe,
            OfferType = PaymentOfferTypes.Recurring,
            LinkLabel = "Betal automatisk",
            ProductName = "Kontingent",
            ProviderProductId = "prod_membership",
            ProviderPriceId = "price_old",
            AmountMinor = 15000,
            Currency = "DKK",
            BillingInterval = "YEAR",
            BillingIntervalCount = 1,
            IsActive = true,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.PaymentOffers.Add(offer);
        await db.SaveChangesAsync();

        db.PaymentSubscriptions.AddRange(
            new PaymentSubscription
            {
                UserId = "user-1",
                PaymentOfferId = offer.Id,
                Provider = PaymentOfferProviders.Stripe,
                ProviderSubscriptionId = "sub-active",
                ProviderProductId = "prod_membership",
                ProviderPriceId = "price_old",
                Status = PaymentSubscriptionStatuses.Active,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            },
            new PaymentSubscription
            {
                UserId = "user-2",
                PaymentOfferId = offer.Id,
                Provider = PaymentOfferProviders.Stripe,
                ProviderSubscriptionId = "sub-pending",
                ProviderProductId = "prod_membership",
                ProviderPriceId = "price_old",
                Status = PaymentSubscriptionStatuses.Pending,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            },
            new PaymentSubscription
            {
                UserId = "user-3",
                PaymentOfferId = offer.Id,
                Provider = PaymentOfferProviders.Stripe,
                ProviderSubscriptionId = "sub-canceled",
                ProviderProductId = "prod_membership",
                ProviderPriceId = "price_old",
                Status = PaymentSubscriptionStatuses.Canceled,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var product = new Stripe.Product { Id = "prod_membership", Name = "Kontingent" };
        var oldPrice = new Stripe.Price
        {
            Id = "price_old",
            ProductId = product.Id,
            Active = true,
            Type = "recurring",
            UnitAmount = 15000,
            Currency = "dkk"
        };
        var stripe = new Mock<IStripeService>();
        stripe.Setup(value => value.GetProductAsync(product.Id)).ReturnsAsync(product);
        stripe.Setup(value => value.GetPriceByIdAsync(oldPrice.Id)).ReturnsAsync(oldPrice);
        stripe.Setup(value => value.CreateRecurringPriceAsync(
                product.Id,
                17500,
                "dkk",
                "year",
                1,
                "Kontingent - RECURRING"))
            .ReturnsAsync(new Stripe.Price
            {
                Id = "price_new",
                ProductId = product.Id,
                Active = true,
                Type = "recurring",
                UnitAmount = 17500,
                Currency = "dkk"
            });
        stripe.Setup(value => value.UpdateSubscriptionPriceAsync(
                It.IsAny<string>(),
                "price_new"))
            .ReturnsAsync(true);
        stripe.Setup(value => value.DeactivatePriceAsync("price_old"))
            .ReturnsAsync(true);

        var controller = new ApiPaymentOffersController(
            db,
            stripe.Object,
            new Mock<IAccessControlService>().Object);

        var result = Assert.IsType<OkObjectResult>(await controller.UpdateForAdmin(offer.Id, new UpdatePaymentOfferRequest
        {
            LinkLabel = "Betal automatisk",
            ProductName = "Kontingent",
            Amount = 175,
            BillingInterval = "YEAR",
            BillingIntervalCount = 1,
            IsActive = true
        }));

        Assert.IsType<PaymentOfferDto>(result.Value);
        stripe.Verify(value => value.CreateRecurringPriceAsync(
            product.Id,
            17500,
            "dkk",
            "year",
            1,
            "Kontingent - RECURRING"), Times.Once);
        stripe.Verify(value => value.UpdateSubscriptionPriceAsync("sub-active", "price_new"), Times.Once);
        stripe.Verify(value => value.UpdateSubscriptionPriceAsync("sub-pending", "price_new"), Times.Once);
        stripe.Verify(value => value.UpdateSubscriptionPriceAsync("sub-canceled", It.IsAny<string>()), Times.Never);
        stripe.Verify(value => value.DeactivatePriceAsync("price_old"), Times.Once);

        var updatedOffer = await db.PaymentOffers.SingleAsync();
        Assert.Equal("price_new", updatedOffer.ProviderPriceId);
        var subscriptions = await db.PaymentSubscriptions.OrderBy(value => value.Id).ToListAsync();
        Assert.Equal("price_new", subscriptions[0].ProviderPriceId);
        Assert.Equal("price_new", subscriptions[1].ProviderPriceId);
        Assert.Equal("price_old", subscriptions[2].ProviderPriceId);
    }

    [Fact]
    public async Task CheckoutCreatesStripeReferencesForMembershipOffer()
    {
        await using var db = CreateDatabase();
        db.Users.Add(new SiteUser
        {
            Id = "user-1",
            UserName = "member@example.com",
            Status = UserStatus.OPRETTET
        });
        var offer = new PaymentOffer
        {
            Scope = PaymentOfferScopes.Membership,
            Provider = PaymentOfferProviders.Stripe,
            OfferType = PaymentOfferTypes.OneTime,
            LinkLabel = "Betal én gang",
            ProductName = "Kontingent",
            AmountMinor = 15000,
            Currency = "DKK",
            IsActive = true,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.PaymentOffers.Add(offer);
        await db.SaveChangesAsync();

        var product = new Stripe.Product { Id = "prod_membership", Name = "Kontingent" };
        var price = new Stripe.Price
        {
            Id = "price_membership",
            ProductId = product.Id,
            Active = true,
            Type = "one_time",
            UnitAmount = 15000,
            Currency = "dkk"
        };
        var stripe = new Mock<IStripeService>();
        stripe.Setup(value => value.GetProductByExactNameAsync("Kontingent")).ReturnsAsync(product);
        stripe.Setup(value => value.GetPriceAsync(product.Id)).ReturnsAsync((Stripe.Price)null!);
        stripe.Setup(value => value.CreateOneTimePriceAsync(
                product.Id,
                15000,
                "dkk",
                "Kontingent - ONE_TIME"))
            .ReturnsAsync(price);
        stripe.Setup(value => value.GetProductAsync(product.Id)).ReturnsAsync(product);
        stripe.Setup(value => value.GetPriceByIdAsync(price.Id)).ReturnsAsync(price);
        stripe.Setup(value => value.StartPayment(
                product,
                price,
                "user-1",
                It.IsAny<string>(),
                It.IsAny<string>(),
                "payment",
                It.IsAny<System.Collections.Generic.IDictionary<string, string>>(),
                It.IsAny<string>()))
            .Returns("cs_membership");

        var controller = new ApiPaymentOffersController(
            db,
            stripe.Object,
            new Mock<IAccessControlService>().Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "user-1")
                }, "test"))
            }
        };

        var result = Assert.IsType<OkObjectResult>(await controller.Checkout(offer.Id, default));

        Assert.Contains("cs_membership", result.Value?.ToString());
        var savedOffer = await db.PaymentOffers.SingleAsync();
        Assert.Equal(product.Id, savedOffer.ProviderProductId);
        Assert.Equal(price.Id, savedOffer.ProviderPriceId);
    }

    [Fact]
    public async Task CheckoutReusesClosedMembershipSubscriptionForMobilePayRecurringOffer()
    {
        await using var db = CreateDatabase();
        db.Users.Add(new SiteUser
        {
            Id = "user-1",
            UserName = "member@example.com",
            PhoneNumber = "+4512345678",
            Status = UserStatus.OPRETTET
        });
        db.PaymentSubscriptions.Add(new PaymentSubscription
        {
            UserId = "user-1",
            Provider = PaymentOfferProviders.MobilePay,
            ProviderSubscriptionId = "old-agreement",
            ProductName = "Kontingent",
            Status = PaymentSubscriptionStatuses.Canceled,
            Created = DateTime.UtcNow.AddDays(-1),
            Updated = DateTime.UtcNow.AddDays(-1)
        });
        var offer = new PaymentOffer
        {
            Scope = PaymentOfferScopes.Membership,
            Provider = PaymentOfferProviders.MobilePay,
            OfferType = PaymentOfferTypes.Recurring,
            LinkLabel = "Betal automatisk",
            ProductName = "Kontingent",
            AmountMinor = 15000,
            Currency = "DKK",
            BillingInterval = "YEAR",
            BillingIntervalCount = 1,
            IsActive = true,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.PaymentOffers.Add(offer);
        await db.SaveChangesAsync();

        var mobilePay = new Mock<IMobilePayService>();
        mobilePay.Setup(value => value.CreateRecurringAgreementAsync(
                "membership-reference",
                "+4512345678",
                15000,
                "DKK",
                "YEAR",
                1,
                "Kontingent",
                "Automatisk betaling for Kontingent",
                "https://localhost:5001/react/pay/success?reference=membership-reference",
                "https://localhost:5001/react/account/manage/subscriptions",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MobilePayAgreement("new-agreement", "https://vipps.example/confirm", "CREATED", "charge-1"));
        var reference = new Mock<IMobilePayEventReferenceService>();
        reference.Setup(value => value.CreateMembership("user-1", offer.Id, It.IsAny<string>())).Returns("membership-reference");

        var controller = new ApiPaymentOffersController(
            db,
            new Mock<IStripeService>().Object,
            new Mock<IAccessControlService>().Object,
            mobilePay.Object,
            reference.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "user-1")
                }, "test"))
            }
        };
        controller.HttpContext.Request.Scheme = "https";
        controller.HttpContext.Request.Host = new HostString("localhost:5001");

        var result = Assert.IsType<OkObjectResult>(await controller.Checkout(offer.Id, default));

        Assert.Contains("vipps.example/confirm", result.Value?.ToString());
        var subscription = await db.PaymentSubscriptions.SingleAsync();
        Assert.Equal("new-agreement", subscription.ProviderSubscriptionId);
        Assert.Equal(PaymentSubscriptionStatuses.Pending, subscription.Status);
    }

    [Fact]
    public async Task SuccessContextMarksCompletedStripeMembershipPaymentAsPaid()
    {
        await using var db = CreateDatabase();
        db.Users.Add(new SiteUser
        {
            Id = "user-1",
            UserName = "member@example.com",
            Status = UserStatus.OPRETTET
        });
        db.PaymentOffers.Add(new PaymentOffer
        {
            Scope = PaymentOfferScopes.Membership,
            Provider = PaymentOfferProviders.Stripe,
            OfferType = PaymentOfferTypes.OneTime,
            ProductName = "Kontingent",
            IsActive = true,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var offer = await db.PaymentOffers.SingleAsync();
        var session = new Session
        {
            Id = "cs_membership",
            Mode = "payment",
            ClientReferenceId = "user-1",
            Created = DateTime.UtcNow,
            Metadata = new System.Collections.Generic.Dictionary<string, string>
            {
                ["PaymentOfferId"] = offer.Id.ToString()
            }
        };
        var stripe = new Mock<IStripeService>();
        stripe.Setup(value => value.GetSessionAsync("cs_membership")).ReturnsAsync(session);
        stripe.Setup(value => value.IsPaymentComplete(session)).Returns(true);

        var controller = new ApiPaymentOffersController(
            db,
            stripe.Object,
            new Mock<IAccessControlService>().Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "user-1")
                }, "test"))
            }
        };

        var result = Assert.IsType<OkObjectResult>(await controller.GetSuccessContext("cs_membership"));

        Assert.Contains("MEMBERSHIP", result.Value?.ToString());
        var user = await db.Users.SingleAsync();
        Assert.Equal(UserStatus.BETALT, user.Status);
    }

    private static ApplicationDbContext CreateDatabase()
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static async Task<PaymentOffer> SeedOfferAsync(ApplicationDbContext db)
    {
        var item = new ContentItem
        {
            Type = ContentTypes.Event,
            Status = ContentStatuses.Published,
            Title = "Test event",
            Slug = "test-event",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        var offer = new PaymentOffer
        {
            ContentItem = item,
            Scope = PaymentOfferScopes.Event,
            Provider = PaymentOfferProviders.Stripe,
            OfferType = PaymentOfferTypes.OneTime,
            ProductName = "Test product",
            ProviderPriceId = "price_event",
            IsActive = true,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.PaymentOffers.Add(offer);
        await db.SaveChangesAsync();
        item.Links.Add(new ContentLink
        {
            Label = "Betalingslink",
            Url = $"/react/events/test-event/pay/{offer.Id}",
            Type = "PAYMENT",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        });
        item.Links.Add(new ContentLink
        {
            Label = "Ekstern betaling",
            Url = "https://tickets.example.com/event",
            Type = "PAYMENT",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return offer;
    }
}
