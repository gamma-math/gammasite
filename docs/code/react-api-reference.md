# React API Reference

This document describes the backend APIs currently used by the React pages. The React client calls these endpoints through `src/frontend/services/api.js`, unless stated otherwise.

## Conventions

- **Method** is the HTTP method used by the client.
- **Auth** describes the main access requirement. Mutating API calls also use the ASP.NET antiforgery token through `X-CSRF-TOKEN`.
- **Data source** identifies the database tables, Identity tables, file system, or external service used by the endpoint.
- Identity tables use the standard ASP.NET Core Identity names, including `AspNetUsers`, `AspNetRoles`, and `AspNetUserRoles`.

## Current user and security

| Method | Endpoint | React pages | Data source | Purpose |
|---|---|---|---|---|
| GET | `/api/me` | Shared application shell and header | `AspNetUsers`, Identity roles and claims | Returns the current authentication state and roles. |
| GET | `/api/account/csrf-token` | Shared API request helper | ASP.NET antiforgery system | Returns the token required for state-changing requests. |

## Account API

| Method | Endpoint | React pages | Data source | Purpose |
|---|---|---|---|---|
| POST | `/api/account/login` | Login page | `AspNetUsers`, Identity login data | Authenticates a user and creates the application cookie. |
| POST | `/api/account/register` | Registration page | `AspNetUsers`, Identity tables | Creates a new user account and starts email confirmation. |
| POST | `/api/account/logout` | Account management page | Authentication cookie | Signs out the current user. |
| POST | `/api/account/forgot-password` | Forgot password page | `AspNetUsers` | Starts the password reset flow for a confirmed email. |
| POST | `/api/account/resend-email-confirmation` | Resend confirmation page | `AspNetUsers` | Sends a new email confirmation link. |
| GET | `/api/account/profile` | Account management page | `AspNetUsers` | Loads the signed-in user profile. |
| PUT | `/api/account/profile` | Account management page | `AspNetUsers` | Updates profile and membership fields. |
| POST | `/api/account/change-email` | Account management page | `AspNetUsers` | Changes the email address and resets confirmation state as required. |
| POST | `/api/account/send-verification-email` | Account management page | `AspNetUsers` | Sends verification for the current email address. |
| POST | `/api/account/change-password` | Account management page | `AspNetUsers` and Identity password data | Changes the signed-in user's password. |
| POST | `/api/account/delete` | Account management page | `AspNetUsers` and related Identity data | Deletes the current account after password confirmation. |

## Content API

| Method | Endpoint | React pages | Data source | Purpose |
|---|---|---|---|---|
| GET | `/api/content` | Front page, events page, news page | `ContentItems`, `ContentLinks` | Lists published events or news. Supports `type` and `frontPage=true`. |
| GET | `/api/content/slug/{slug}` | Event and news detail pages, registrations page | `ContentItems`, `ContentLinks` | Loads one published content item by slug. |
| GET | `/api/content/admin` | Admin events and news pages, message composer | `ContentItems`, `ContentLinks` | Lists content for administration or message selection. Supports type and status filters. |
| POST | `/api/content` | Admin event/news create page | `ContentItems`, `ContentLinks` | Creates an event or news item. **Admin only.** |
| PUT | `/api/content/{id}` | Admin event/news edit page | `ContentItems`, `ContentLinks` | Updates an event or news item. **Admin only.** |
| DELETE | `/api/content/{id}` | Admin event/news page | `ContentItems`, `ContentLinks` | Deletes content and its links. **Admin only.** |

## Event registration API

| Method | Endpoint | React pages | Data source | Purpose |
|---|---|---|---|---|
| GET | `/api/content/{id}/registrations/me` | Event detail page | `EventRegistrations`, `ContentItems`, `AspNetUsers` | Loads the current user's registration. |
| POST | `/api/content/{id}/registrations` | Event detail page | `EventRegistrations`, `ContentItems`, `AspNetUsers` | Registers the current user for an open event. |
| DELETE | `/api/content/{id}/registrations/me` | Event detail page | `EventRegistrations`, `ContentItems`, `AspNetUsers` | Removes the current user's registration from an open event. |
| GET | `/api/content/{id}/registrations` | Event detail and registrations pages | `EventRegistrations`, `AspNetUsers` | Lists attendees and registration states. |
| POST | `/api/content/{id}/registrations/admin` | Event registrations page | `EventRegistrations`, `AspNetUsers`, `ContentItems` | Adds a member to an event manually. **Admin only.** |
| PUT | `/api/content/{id}/registrations/{registrationId}` | Event registrations page | `EventRegistrations`, `AspNetUsers` | Updates an attendee's registration type or response. **Admin only.** |

## Member API

| Method | Endpoint | React pages | Data source | Purpose |
|---|---|---|---|---|
| GET | `/api/members` | Member directory | `AspNetUsers` | Lists confirmed members whose status is neither `INAKTIV` nor `OPRETTET`. Visibility controls whether private profile fields are included. |
| GET | `/api/members/admin` | Admin members, event registrations, message composer | `AspNetUsers` | Lists all users for administration and recipient selection. **Admin only.** |
| PUT | `/api/members/{id}/status` | Admin members page | `AspNetUsers` | Changes one user's membership status. **Admin only.** |
| POST | `/api/members/admin/mass-status` | Admin members page | `AspNetUsers` | Changes status for users in a selected date range. **Admin only.** |

## Role API

| Method | Endpoint | React pages | Data source | Purpose |
|---|---|---|---|---|
| GET | `/api/roles` | Admin roles page, message composer | `AspNetRoles` | Lists application roles. **Admin only.** |
| POST | `/api/roles` | Admin role create page | `AspNetRoles` | Creates a role. **Admin only.** |
| DELETE | `/api/roles/{id}` | Admin roles page | `AspNetRoles`, `AspNetUserRoles` | Deletes a role unless it is protected. **Admin only.** |
| GET | `/api/roles/{id}/members` | Admin role edit page | `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles` | Returns members and non-members for one role. **Admin only.** |
| PUT | `/api/roles/{id}/members` | Admin role edit page | `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles` | Adds and removes users from a role. **Admin only.** |

## Email template API

| Method | Endpoint | React pages | Data source | Purpose |
|---|---|---|---|---|
| GET | `/api/email-templates` | Admin templates page, message composer | `EmailTemplates` | Lists reusable templates, optionally filtered by type and active state. **Admin only.** |
| GET | `/api/email-templates/{id}` | Admin template editor | `EmailTemplates` | Loads one template. **Admin only.** |
| POST | `/api/email-templates` | Admin template create page | `EmailTemplates` | Creates a template. **Admin only.** |
| PUT | `/api/email-templates/{id}` | Admin template edit page | `EmailTemplates` | Updates a template. **Admin only.** |
| DELETE | `/api/email-templates/{id}` | Admin templates page | `EmailTemplates` | Deletes a template. **Admin only.** |
| POST | `/api/email-templates/{id}/preview` | Admin template editor | `EmailTemplates` | Renders a template preview using supplied values. **Admin only.** |

## Message API

| Method | Endpoint | React pages | Data source | Purpose |
|---|---|---|---|---|
| GET | `/api/messages/categories` | Admin messages page | `AspNetUsers`, `AspNetRoles` | Returns available member statuses and roles for recipient filters. **Admin only.** |
| POST | `/api/messages/recipient-preview` | Admin messages page | `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `EventRegistrations` | Resolves selected groups, roles, event attendees, and specific members into recipient counts and previews. **Admin only.** |
| POST | `/api/messages/render` | Admin messages page | `EmailTemplates`, `ContentItems`, `ContentLinks` | Renders the selected template and content blocks before sending. **Admin only.** |
| POST | `/api/messages/send` | Admin messages page | `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `EventRegistrations`, `EmailTemplates` | Resolves recipients and sends email and/or SMS through the configured services. **Admin only.** |

## Calendar and library API

| Method | Endpoint | React pages | Data source | Purpose |
|---|---|---|---|---|
| GET | `/api/calendar` | Member calendar page | External iCal feed configured by `ICalService` | Loads upcoming calendar events and maps them to the React calendar model. |
| GET | `/api/library` | Member library page | Server file system through `IIndexService` | Lists protected documents and folders for the requested path. |

## Payment API

| Method | Endpoint | React pages | Data source | Purpose |
|---|---|---|---|---|
| GET | `/api/payments/config` | Payment page and generic payment page | Application configuration | Returns the public Stripe API key. |
| GET | `/api/payments/products` | Payment page | External Stripe Products and Prices | Lists available membership or payment products. |
| GET | `/api/payments/products/{id}` | Product payment page | External Stripe Product and Price | Loads one Stripe product and its price. |
| POST | `/api/Stripe/Product` | Product payment page | External Stripe Checkout | Creates a checkout session for a Stripe product. |
| POST | `/api/Stripe/Generic` | Generic payment page | External Stripe Checkout | Creates a checkout session for a custom amount and description. |

## Rich text editor API

| Method | Endpoint | React pages | Data source | Purpose |
|---|---|---|---|---|
| POST | `/api/editor/images` | Admin event/news editor, admin message editor, admin template editor | `wwwroot/uploads/editor` file system | Validates and stores an uploaded editor image and returns its public URL. **Admin only.** |

## Shared request behavior

All React API requests use same-origin credentials so the ASP.NET authentication cookie is included. `GET` requests do not require an antiforgery header. `POST`, `PUT`, and `DELETE` requests automatically obtain `/api/account/csrf-token` and send the returned token in `X-CSRF-TOKEN`, except for multipart image uploads, which use the same token through `FormData`.
