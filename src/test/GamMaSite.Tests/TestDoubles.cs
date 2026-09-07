using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using GamMaSite.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Microsoft.AspNetCore.Identity;

namespace GamMaSite.Tests;

/*
 * Provides shared mocks and authenticated test principals for Identity-based tests.
 * Keeps UserManager, SignInManager, RoleManager, and page context setup consistent.
 */
internal static class TestDoubles
{
    public static Mock<UserManager<SiteUser>> UserManager()
    {
        var store = new Mock<IUserStore<SiteUser>>();
        return new Mock<UserManager<SiteUser>>(
            store.Object,
            new Mock<IOptions<IdentityOptions>>().Object,
            new Mock<IPasswordHasher<SiteUser>>().Object,
            Array.Empty<IUserValidator<SiteUser>>(),
            Array.Empty<IPasswordValidator<SiteUser>>(),
            new Mock<ILookupNormalizer>().Object,
            new IdentityErrorDescriber(),
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<SiteUser>>>().Object);
    }

    public static Mock<SignInManager<SiteUser>> SignInManager(Mock<UserManager<SiteUser>> userManager)
    {
        return new Mock<SignInManager<SiteUser>>(
            userManager.Object,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<SiteUser>>().Object,
            new Mock<IOptions<IdentityOptions>>().Object,
            new Mock<ILogger<SignInManager<SiteUser>>>().Object,
            new Mock<IAuthenticationSchemeProvider>().Object,
            new Mock<IUserConfirmation<SiteUser>>().Object);
    }

    public static Mock<RoleManager<IdentityRole>> RoleManager()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        return new Mock<RoleManager<IdentityRole>>(
            store.Object,
            Array.Empty<IRoleValidator<IdentityRole>>(),
            new Mock<ILookupNormalizer>().Object,
            new IdentityErrorDescriber(),
            new Mock<ILogger<RoleManager<IdentityRole>>>().Object);
    }

    public static ClaimsPrincipal User(string id = "user-1", params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, id) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    public static PageContext PageContext(ClaimsPrincipal user)
    {
        return new PageContext { HttpContext = new DefaultHttpContext { User = user } };
    }
}
