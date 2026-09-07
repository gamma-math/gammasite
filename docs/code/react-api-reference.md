# React API Reference

This document describes the backend APIs currently used by the React pages. The React client calls these endpoints through `src/frontend/services/api.js`, unless stated otherwise.

## Conventions

- **Method** is the HTTP method used by the client.
- **Auth** describes the main access requirement. Mutating API calls also use the ASP.NET antiforgery token through `X-CSRF-TOKEN`.
- **Data source** identifies the database tables, Identity tables, file system, or external service used by the endpoint.
- Identity tables use the standard ASP.NET Core Identity names, including `AspNetUsers`, `AspNetRoles`, and `AspNetUserRoles`.

## Current user and security

| Method | Endpoint | React pages | Data source | Purpose | Tests |
|---|---|---|---|---|---|
| GET | `/api/me` | Shared application shell and header | `AspNetUsers`, Identity roles and claims | Returns the current authentication state and roles. | Not yet covered |
| GET | `/api/account/csrf-token` | Shared API request helper | ASP.NET antiforgery system | Returns the token required for state-changing requests. | Not yet covered |

## Account API

| Method | Endpoint | React pages | Data source | Purpose | Tests |
|---|---|---|---|---|---|
| POST | `/api/account/login` | Login page | `AspNetUsers`, Identity login data | Authenticates a user and creates the application cookie. | [ApiAccountControllerTests.cs](../../src/test/GamMaSite.Tests/ApiAccountControllerTests.cs) |
| POST | `/api/account/register` | Registration page | `AspNetUsers`, Identity tables | Creates a new user account and starts email confirmation. | Not yet covered |
| POST | `/api/account/logout` | Account management page | Authentication cookie | Signs out the current user. | [ApiAccountControllerTests.cs](../../src/test/GamMaSite.Tests/ApiAccountControllerTests.cs) |
| POST | `/api/account/forgot-password` | Forgot password page | `AspNetUsers` | Starts the password reset flow for a confirmed email. | [ApiAccountControllerTests.cs](../../src/test/GamMaSite.Tests/ApiAccountControllerTests.cs) |
| POST | `/api/account/resend-email-confirmation` | Resend confirmation page | `AspNetUsers` | Sends a new email confirmation link. | Not yet covered |
| GET | `/api/account/profile` | Account management page | `AspNetUsers` | Loads the signed-in user profile. | [ApiAccountControllerTests.cs](../../src/test/GamMaSite.Tests/ApiAccountControllerTests.cs) |
| PUT | `/api/account/profile` | Account management page | `AspNetUsers` | Updates profile and membership fields. | [ApiAccountControllerTests.cs](../../src/test/GamMaSite.Tests/ApiAccountControllerTests.cs) |
| POST | `/api/account/change-email` | Account management page | `AspNetUsers` | Changes the email address and resets confirmation state as required. | [ApiAccountControllerTests.cs](../../src/test/GamMaSite.Tests/ApiAccountControllerTests.cs) |
| POST | `/api/account/send-verification-email` | Account management page | `AspNetUsers` | Sends verification for the current email address. | Not yet covered |
| POST | `/api/account/change-password` | Account management page | `AspNetUsers` and Identity password data | Changes the signed-in user's password. | [ApiAccountControllerTests.cs](../../src/test/GamMaSite.Tests/ApiAccountControllerTests.cs) |
| POST | `/api/account/delete` | Account management page | `AspNetUsers` and related Identity data | Deletes the current account after password confirmation. | Not yet covered |

## Content API

| Method | Endpoint | React pages | Data source | Purpose | Tests |
|---|---|---|---|---|---|
| GET | `/api/content` | Front page, events page, news page | `ContentItems`, `ContentLinks` | Lists published events or news. Supports `type` and `frontPage=true`. | [ApiContentControllerTests.cs](../../src/test/GamMaSite.Tests/ApiContentControllerTests.cs), [ContentServiceTests.cs](../../src/test/GamMaSite.Tests/ContentServiceTests.cs) |
| GET | `/api/content/slug/{slug}` | Event and news detail pages, registrations page | `ContentItems`, `ContentLinks` | Loads one published content item by slug. | Not yet covered |
| GET | `/api/content/admin` | Admin events and news pages, message composer | `ContentItems`, `ContentLinks` | Lists content for administration or message selection. Supports type and status filters. | Not yet covered |
| POST | `/api/content` | Admin event/news create page | `ContentItems`, `ContentLinks` | Creates an event or news item. **Admin only.** | [ApiContentControllerTests.cs](../../src/test/GamMaSite.Tests/ApiContentControllerTests.cs), [ContentServiceTests.cs](../../src/test/GamMaSite.Tests/ContentServiceTests.cs) |
| PUT | `/api/content/{id}` | Admin event/news edit page | `ContentItems`, `ContentLinks` | Updates an event or news item. **Admin only.** | Not yet covered |
| DELETE | `/api/content/{id}` | Admin event/news page | `ContentItems`, `ContentLinks` | Deletes content and its links. **Admin only.** | Not yet covered |

## Event registration API

| Method | Endpoint | React pages | Data source | Purpose | Tests |
|---|---|---|---|---|---|
| GET | `/api/content/{id}/registrations/me` | Event detail page | `EventRegistrations`, `ContentItems`, `AspNetUsers` | Loads the current user's registration. | Not yet covered |
| POST | `/api/content/{id}/registrations` | Event detail page | `EventRegistrations`, `ContentItems`, `AspNetUsers` | Registers the current user for an open event. | [ApiContentControllerTests.cs](../../src/test/GamMaSite.Tests/ApiContentControllerTests.cs), [EventRegistrationServiceTests.cs](../../src/test/GamMaSite.Tests/EventRegistrationServiceTests.cs) |
| DELETE | `/api/content/{id}/registrations/me` | Event detail page | `EventRegistrations`, `ContentItems`, `AspNetUsers` | Removes the current user's registration from an open event. | [EventRegistrationServiceTests.cs](../../src/test/GamMaSite.Tests/EventRegistrationServiceTests.cs) |
| GET | `/api/content/{id}/registrations` | Event detail and registrations pages | `EventRegistrations`, `AspNetUsers` | Lists attendees and registration states. | Not yet covered |
| POST | `/api/content/{id}/registrations/admin` | Event registrations page | `EventRegistrations`, `AspNetUsers`, `ContentItems` | Adds a member to an event manually. **Admin only.** | [EventRegistrationServiceTests.cs](../../src/test/GamMaSite.Tests/EventRegistrationServiceTests.cs) |
| PUT | `/api/content/{id}/registrations/{registrationId}` | Event registrations page | `EventRegistrations`, `AspNetUsers` | Updates an attendee's registration type or response. **Admin only.** | [EventRegistrationServiceTests.cs](../../src/test/GamMaSite.Tests/EventRegistrationServiceTests.cs) |

## Member API

| Method | Endpoint | React pages | Data source | Purpose | Tests |
|---|---|---|---|---|---|
| GET | `/api/members` | Member directory | `AspNetUsers` | Lists confirmed members whose status is neither `INAKTIV` nor `OPRETTET`. Visibility controls whether private profile fields are included. | [ApiMembersControllerTests.cs](../../src/test/GamMaSite.Tests/ApiMembersControllerTests.cs) |
| GET | `/api/members/admin` | Admin members, event registrations, message composer | `AspNetUsers` | Lists all users for administration and recipient selection. **Admin only.** | Not yet covered |
| PUT | `/api/members/{id}/status` | Admin members page | `AspNetUsers` | Changes one user's membership status. **Admin only.** | [ApiMembersControllerTests.cs](../../src/test/GamMaSite.Tests/ApiMembersControllerTests.cs) |
| POST | `/api/members/admin/mass-status` | Admin members page | `AspNetUsers` | Changes status for users in a selected date range. **Admin only.** | [ApiMembersControllerTests.cs](../../src/test/GamMaSite.Tests/ApiMembersControllerTests.cs) |

## Role API

| Method | Endpoint | React pages | Data source | Purpose | Tests |
|---|---|---|---|---|---|
| GET | `/api/roles` | Admin roles page, message composer | `AspNetRoles` | Lists application roles. **Admin only.** | [ApiRolesControllerTests.cs](../../src/test/GamMaSite.Tests/ApiRolesControllerTests.cs) |
| POST | `/api/roles` | Admin role create page | `AspNetRoles` | Creates a role. **Admin only.** | [ApiRolesControllerTests.cs](../../src/test/GamMaSite.Tests/ApiRolesControllerTests.cs) |
| DELETE | `/api/roles/{id}` | Admin roles page | `AspNetRoles`, `AspNetUserRoles` | Deletes a role unless it is protected. **Admin only.** | [ApiRolesControllerTests.cs](../../src/test/GamMaSite.Tests/ApiRolesControllerTests.cs) |
| GET | `/api/roles/{id}/members` | Admin role edit page | `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles` | Returns members and non-members for one role. **Admin only.** | Not yet covered |
| PUT | `/api/roles/{id}/members` | Admin role edit page | `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles` | Adds and removes users from a role. **Admin only.** | Not yet covered |

## Email template API

| Method | Endpoint | React pages | Data source | Purpose | Tests |
|---|---|---|---|---|---|
| GET | `/api/email-templates` | Admin templates page, message composer | `EmailTemplates` | Lists reusable templates, optionally filtered by type and active state. **Admin only.** | [EmailTemplateServiceTests.cs](../../src/test/GamMaSite.Tests/EmailTemplateServiceTests.cs) |
| GET | `/api/email-templates/{id}` | Admin template editor | `EmailTemplates` | Loads one template. **Admin only.** | [ApiEmailTemplatesControllerTests.cs](../../src/test/GamMaSite.Tests/ApiEmailTemplatesControllerTests.cs) |
| POST | `/api/email-templates` | Admin template create page | `EmailTemplates` | Creates a template. **Admin only.** | [ApiEmailTemplatesControllerTests.cs](../../src/test/GamMaSite.Tests/ApiEmailTemplatesControllerTests.cs), [EmailTemplateServiceTests.cs](../../src/test/GamMaSite.Tests/EmailTemplateServiceTests.cs) |
| PUT | `/api/email-templates/{id}` | Admin template edit page | `EmailTemplates` | Updates a template. **Admin only.** | [ApiEmailTemplatesControllerTests.cs](../../src/test/GamMaSite.Tests/ApiEmailTemplatesControllerTests.cs) |
| DELETE | `/api/email-templates/{id}` | Admin templates page | `EmailTemplates` | Deletes a template. **Admin only.** | [ApiEmailTemplatesControllerTests.cs](../../src/test/GamMaSite.Tests/ApiEmailTemplatesControllerTests.cs) |
| POST | `/api/email-templates/{id}/preview` | Admin template editor | `EmailTemplates` | Renders a template preview using supplied values. **Admin only.** | [EmailTemplateServiceTests.cs](../../src/test/GamMaSite.Tests/EmailTemplateServiceTests.cs) |

## Message API

| Method | Endpoint | React pages | Data source | Purpose | Tests |
|---|---|---|---|---|---|
| GET | `/api/messages/categories` | Admin messages page | `AspNetUsers`, `AspNetRoles` | Returns available member statuses and roles for recipient filters. **Admin only.** | [ApiMessagesControllerTests.cs](../../src/test/GamMaSite.Tests/ApiMessagesControllerTests.cs) |
| POST | `/api/messages/recipient-preview` | Admin messages page | `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `EventRegistrations` | Resolves selected groups, roles, event attendees, and specific members into recipient counts and previews. **Admin only.** | [ApiMessagesControllerTests.cs](../../src/test/GamMaSite.Tests/ApiMessagesControllerTests.cs) |
| POST | `/api/messages/render` | Admin messages page | `EmailTemplates`, `ContentItems`, `ContentLinks` | Renders the selected template and content blocks before sending. **Admin only.** | Not yet covered |
| POST | `/api/messages/send` | Admin messages page | `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `EventRegistrations`, `EmailTemplates` | Resolves recipients and sends email and/or SMS through the configured services. **Admin only.** | [ApiMessagesControllerTests.cs](../../src/test/GamMaSite.Tests/ApiMessagesControllerTests.cs) |

## Calendar and library API

| Method | Endpoint | React pages | Data source | Purpose | Tests |
|---|---|---|---|---|---|
| GET | `/api/calendar` | Member calendar page | External iCal feed configured by `ICalService` | Loads upcoming calendar events and maps them to the React calendar model. | [ApiLibraryAndCalendarControllerTests.cs](../../src/test/GamMaSite.Tests/ApiLibraryAndCalendarControllerTests.cs) |
| GET | `/api/library` | Member library page | Server file system through `IIndexService` | Lists protected documents and folders for the requested path. | [ApiLibraryAndCalendarControllerTests.cs](../../src/test/GamMaSite.Tests/ApiLibraryAndCalendarControllerTests.cs) |

## Payment API

| Method | Endpoint | React pages | Data source | Purpose | Tests |
|---|---|---|---|---|---|
| GET | `/api/payments/config` | Payment page and generic payment page | Application configuration | Returns the public Stripe API key. | [ApiPaymentsControllerTests.cs](../../src/test/GamMaSite.Tests/ApiPaymentsControllerTests.cs) |
| GET | `/api/payments/products` | Payment page | External Stripe Products and Prices | Lists available membership or payment products. | Not yet covered |
| GET | `/api/payments/products/{id}` | Product payment page | External Stripe Product and Price | Loads one Stripe product and its price. | [ApiPaymentsControllerTests.cs](../../src/test/GamMaSite.Tests/ApiPaymentsControllerTests.cs) |
| POST | `/api/Stripe/Product` | Product payment page | External Stripe Checkout | Creates a checkout session for a Stripe product. | Not yet covered |
| POST | `/api/Stripe/Generic` | Generic payment page | External Stripe Checkout | Creates a checkout session for a custom amount and description. | Not yet covered |

## Rich text editor API

| Method | Endpoint | React pages | Data source | Purpose | Tests |
|---|---|---|---|---|---|
| POST | `/api/editor/images` | Admin event/news editor, admin message editor, admin template editor | `wwwroot/uploads/editor` file system | Validates and stores an uploaded editor image and returns its public URL. **Admin only.** | [ApiEditorControllerTests.cs](../../src/test/GamMaSite.Tests/ApiEditorControllerTests.cs) |

## Shared request behavior

All React API requests use same-origin credentials so the ASP.NET authentication cookie is included. `GET` requests do not require an antiforgery header. `POST`, `PUT`, and `DELETE` requests automatically obtain `/api/account/csrf-token` and send the returned token in `X-CSRF-TOKEN`, except for multipart image uploads, which use the same token through `FormData`.
