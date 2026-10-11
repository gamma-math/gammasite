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
| POST | `/api/content` | Admin event/news create page | `ContentItems`, `ContentLinks` | Creates an event or news item. Requires `content.edit`. | [ApiContentControllerTests.cs](../../src/test/GamMaSite.Tests/ApiContentControllerTests.cs), [ContentServiceTests.cs](../../src/test/GamMaSite.Tests/ContentServiceTests.cs) |
| PUT | `/api/content/{id}` | Admin event/news edit page | `ContentItems`, `ContentLinks` | Updates an event or news item. Requires `content.edit` or organizer access for that event. | Not yet covered |
| DELETE | `/api/content/{id}` | Admin event/news page | `ContentItems`, `ContentLinks` | Deletes content and its links. Requires `content.edit`. | Not yet covered |
| POST | `/api/content/{id}/image` | Event/news edit page | Configured content media folder and `ContentItems` | Validates and stores one PNG/JPEG of at most 2 MiB and updates `PictureUrl`. Requires `content.edit` or organizer access for an event. | [ApiContentControllerTests.cs](../../src/test/GamMaSite.Tests/ApiContentControllerTests.cs), [ContentMediaServiceTests.cs](../../src/test/GamMaSite.Tests/ContentMediaServiceTests.cs) |
| DELETE | `/api/content/{id}/image` | Event/news edit page | Configured content media folder and `ContentItems` | Removes the image reference and deletes an unreferenced local upload. External URLs are only detached. | [ApiContentControllerTests.cs](../../src/test/GamMaSite.Tests/ApiContentControllerTests.cs), [ContentMediaServiceTests.cs](../../src/test/GamMaSite.Tests/ContentMediaServiceTests.cs) |

## Event registration API

| Method | Endpoint | React pages | Data source | Purpose | Tests |
|---|---|---|---|---|---|
| GET | `/api/content/{id}/registrations/me` | Event detail page | `EventRegistrations`, `ContentItems`, `AspNetUsers` | Loads the current user's registration. | Not yet covered |
| POST | `/api/content/{id}/registrations` | Event detail page | `EventRegistrations`, `ContentItems`, `AspNetUsers` | Registers the current user for an open event. | [ApiContentControllerTests.cs](../../src/test/GamMaSite.Tests/ApiContentControllerTests.cs), [EventRegistrationServiceTests.cs](../../src/test/GamMaSite.Tests/EventRegistrationServiceTests.cs) |
| DELETE | `/api/content/{id}/registrations/me` | Event detail page | `EventRegistrations`, `ContentItems`, `AspNetUsers` | Removes the current user's registration from an open event. | [EventRegistrationServiceTests.cs](../../src/test/GamMaSite.Tests/EventRegistrationServiceTests.cs) |
| GET | `/api/content/{id}/registrations` | Event detail and registrations pages | `EventRegistrations`, `AspNetUsers` | Lists attendees and registration states. | Not yet covered |
| POST | `/api/content/{id}/registrations/admin` | Event registrations page | `EventRegistrations`, `AspNetUsers`, `ContentItems` | Adds a member to an event manually. Requires `content.edit`, `registrations.edit`, or organizer access for that event. | [EventRegistrationServiceTests.cs](../../src/test/GamMaSite.Tests/EventRegistrationServiceTests.cs) |
| PUT | `/api/content/{id}/registrations/{registrationId}` | Event registrations page | `EventRegistrations`, `AspNetUsers` | Updates an attendee's registration type or response. Requires `content.edit`, `registrations.edit`, or organizer access for that event. | [EventRegistrationServiceTests.cs](../../src/test/GamMaSite.Tests/EventRegistrationServiceTests.cs) |

## Member API

| Method | Endpoint | React pages | Data source | Purpose | Tests |
|---|---|---|---|---|---|
| GET | `/api/members` | Member directory | `AspNetUsers` | Lists confirmed members whose status is neither `INAKTIV` nor `OPRETTET`. Visibility controls whether private profile fields are included. | [ApiMembersControllerTests.cs](../../src/test/GamMaSite.Tests/ApiMembersControllerTests.cs) |
| GET | `/api/members/admin` | Admin members, event registrations, message composer | `AspNetUsers` | Lists all users for administration and recipient selection. Requires the relevant member-data permission. | Not yet covered |
| GET | `/api/members/finance` | Admin finance pages | `AspNetUsers` | Lists only member IDs and names for finance user labels and filters. Requires `finance.view.all` or `finance.edit.all`. | [ApiMembersControllerTests.cs](../../src/test/GamMaSite.Tests/ApiMembersControllerTests.cs) |
| PUT | `/api/members/{id}/status` | Admin members page | `AspNetUsers` | Changes one user's membership status. Requires `roles.edit`. | [ApiMembersControllerTests.cs](../../src/test/GamMaSite.Tests/ApiMembersControllerTests.cs) |
| POST | `/api/members/admin/mass-status` | Admin members page | `AspNetUsers` | Changes status for users in a selected date range. Requires `roles.edit`. | [ApiMembersControllerTests.cs](../../src/test/GamMaSite.Tests/ApiMembersControllerTests.cs) |

## Role API

| Method | Endpoint | React pages | Data source | Purpose | Tests |
|---|---|---|---|---|---|
| GET | `/api/roles` | Admin roles page, message composer | `AspNetRoles` | Lists application roles. Requires `roles.edit` or `messages.edit`. | [ApiRolesControllerTests.cs](../../src/test/GamMaSite.Tests/ApiRolesControllerTests.cs) |
| POST | `/api/roles` | Admin role create page | `AspNetRoles` | Creates a role. Requires `roles.edit`. | [ApiRolesControllerTests.cs](../../src/test/GamMaSite.Tests/ApiRolesControllerTests.cs) |
| DELETE | `/api/roles/{id}` | Admin roles page | `AspNetRoles`, `AspNetUserRoles` | Deletes a role unless it is protected. Requires `roles.edit`. | [ApiRolesControllerTests.cs](../../src/test/GamMaSite.Tests/ApiRolesControllerTests.cs) |
| GET | `/api/roles/{id}/members` | Admin role edit page, message composer | `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles` | Returns members and non-members for one role. Requires `roles.edit` or `messages.edit`. | Not yet covered |
| PUT | `/api/roles/{id}/members` | Admin role edit page | `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles` | Adds and removes users from a role. Requires `roles.edit`. | Not yet covered |

## Email template API

| Method | Endpoint | React pages | Data source | Purpose | Tests |
|---|---|---|---|---|---|
| GET | `/api/email-templates` | Admin templates page, message composer | `EmailTemplates` | Lists reusable templates, optionally filtered by type and active state. Requires `email_templates.edit` or `messages.edit`. | [EmailTemplateServiceTests.cs](../../src/test/GamMaSite.Tests/EmailTemplateServiceTests.cs) |
| GET | `/api/email-templates/{id}` | Admin template editor | `EmailTemplates` | Loads one template. Requires `email_templates.edit` or `messages.edit`. | [ApiEmailTemplatesControllerTests.cs](../../src/test/GamMaSite.Tests/ApiEmailTemplatesControllerTests.cs) |
| POST | `/api/email-templates` | Admin template create page | `EmailTemplates` | Creates a template. Requires `email_templates.edit`. | [ApiEmailTemplatesControllerTests.cs](../../src/test/GamMaSite.Tests/ApiEmailTemplatesControllerTests.cs), [EmailTemplateServiceTests.cs](../../src/test/GamMaSite.Tests/EmailTemplateServiceTests.cs) |
| PUT | `/api/email-templates/{id}` | Admin template edit page | `EmailTemplates` | Updates a template. Requires `email_templates.edit`. | [ApiEmailTemplatesControllerTests.cs](../../src/test/GamMaSite.Tests/ApiEmailTemplatesControllerTests.cs) |
| DELETE | `/api/email-templates/{id}` | Admin templates page | `EmailTemplates` | Deletes a template. Requires `email_templates.edit`. | [ApiEmailTemplatesControllerTests.cs](../../src/test/GamMaSite.Tests/ApiEmailTemplatesControllerTests.cs) |
| POST | `/api/email-templates/{id}/preview` | Admin template editor | `EmailTemplates` | Renders a template preview using supplied values. Requires `email_templates.edit` or `messages.edit`. | [EmailTemplateServiceTests.cs](../../src/test/GamMaSite.Tests/EmailTemplateServiceTests.cs) |

## Message API

| Method | Endpoint | React pages | Data source | Purpose | Tests |
|---|---|---|---|---|---|
| GET | `/api/messages/categories` | Admin messages page | `AspNetUsers`, `AspNetRoles` | Returns available member statuses and roles for recipient filters. Requires `messages.edit`. | [ApiMessagesControllerTests.cs](../../src/test/GamMaSite.Tests/ApiMessagesControllerTests.cs) |
| POST | `/api/messages/recipient-preview` | Admin messages page | `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `EventRegistrations` | Resolves selected groups, roles, event attendees, and specific members into recipient counts and previews. Requires `messages.edit`. | [ApiMessagesControllerTests.cs](../../src/test/GamMaSite.Tests/ApiMessagesControllerTests.cs) |
| POST | `/api/messages/render` | Admin messages page | `EmailTemplates`, `ContentItems`, `ContentLinks` | Renders the selected template and content blocks before sending. Requires `messages.edit`. | Not yet covered |
| POST | `/api/messages/send` | Admin messages page | `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `EventRegistrations`, `EmailTemplates` | Resolves recipients and sends email and/or SMS through the configured services. Requires `messages.edit`. | [ApiMessagesControllerTests.cs](../../src/test/GamMaSite.Tests/ApiMessagesControllerTests.cs) |

## Calendar and library API

| Method | Endpoint | React pages | Data source | Purpose | Tests |
|---|---|---|---|---|---|
| GET | `/api/calendar` | Member calendar page | External iCal feed configured by `ICalService` | Loads upcoming calendar events and maps them to the React calendar model. | [ApiLibraryAndCalendarControllerTests.cs](../../src/test/GamMaSite.Tests/ApiLibraryAndCalendarControllerTests.cs) |
| GET | `/api/library` | Member library page | Server file system through `IIndexService` | Lists protected documents and folders for the requested path. | [ApiLibraryAndCalendarControllerTests.cs](../../src/test/GamMaSite.Tests/ApiLibraryAndCalendarControllerTests.cs) |

## Finance API

All Finance endpoints use the PostgreSQL finance database. Admin read endpoints require `finance.view.all` or `finance.edit.all`; admin write endpoints require `finance.edit.all`. The regular overview and postings endpoints require an authenticated user and use the current user's ID when selecting personal transactions.

| Method | Endpoint | React pages | Data source | Purpose | Tests |
|---|---|---|---|---|---|
| GET | `/api/finance/overview?year={year}` | Account finance overview | Finance PostgreSQL tables (`posteringer`, `forecast`, `account`, `postering_group`) | Returns the selected year's income, expenses, monthly totals, budgets, previous-year values, and data-quality summary. The regular user endpoint accepts the current year and the previous year. | [ApiFinanceControllerTests.cs](../../src/test/GamMaSite.Tests/ApiFinanceControllerTests.cs) |
| GET | `/api/finance/postings` | Account transactions page | `posteringer`, finance account and posting-group tables | Returns the signed-in user's transactions and the last finance update timestamp. | Not yet covered |
| GET | `/api/finance/admin/overview?year={year}` | Admin Finance overview | Finance PostgreSQL tables | Returns the admin overview for any year from 1900 through 9999. | [ApiFinanceControllerTests.cs](../../src/test/GamMaSite.Tests/ApiFinanceControllerTests.cs) |
| GET | `/api/finance/admin/budgets` | Admin budgets page | `forecast`, `account`, `postering_group` | Lists all budget rows with full account and posting-group identifiers. | Not yet covered |
| GET | `/api/finance/admin/accounts` | Admin chart of accounts page | `account` | Lists the chart of accounts and its account/context keys. | Not yet covered |
| POST | `/api/finance/admin/budgets` | Admin budgets page | `forecast` and reference tables | Creates a budget row. The body is `FinanceBudgetUpdateDto` (`id`, `accountId`, `postingGroupId`, `yearActual`, `forecast`, `forecastType`). | [FinanceReportServiceTests.cs](../../src/test/GamMaSite.Tests/FinanceReportServiceTests.cs) |
| GET | `/api/finance/admin/budgets/{id}` | Admin budget editor | `forecast` | Loads one budget row by ID. | Not yet covered |
| PUT | `/api/finance/admin/budgets/{id}` | Admin budget editor | `forecast` and reference tables | Updates a budget row and its `updated_at` timestamp. | Not yet covered |
| DELETE | `/api/finance/admin/budgets/{id}` | Admin budgets page | `forecast` | Deletes a budget row. | Not yet covered |
| GET | `/api/finance/admin/posteringsgrupper` | Admin posting groups page | `postering_group` | Lists posting groups and contexts. | Not yet covered |
| GET | `/api/finance/admin/posteringsgrupper/{id}` | Admin posting-group editor | `postering_group` | Loads one posting group. | Not yet covered |
| POST | `/api/finance/admin/posteringsgrupper` | Admin posting-group editor | `postering_group` | Creates a posting group. The body is `FinancePostingGroupUpdateDto` (`id`, `postingGroup`, `context`). | [FinanceReportServiceTests.cs](../../src/test/GamMaSite.Tests/FinanceReportServiceTests.cs) |
| PUT | `/api/finance/admin/posteringsgrupper/{id}` | Admin posting-group editor | `postering_group` | Updates a posting group and its `updated_at` timestamp. | Not yet covered |
| DELETE | `/api/finance/admin/posteringsgrupper/{id}` | Admin posting groups page | `postering_group` | Deletes a posting group. | Not yet covered |
| GET | `/api/finance/admin/postings?year={year}&allYears={bool}&accountId={id}&bankKey={key}&mobilePayKey={key}` | Admin postings and posting editor | `posteringer`, `account`, `postering_group`, `AspNetUsers`, `bank_account`, `mobilepay` | Lists admin transactions. Use `allYears=true` to omit the year filter; otherwise `year` selects a valid year. Account and source keys are optional filters. | [ApiFinanceControllerTests.cs](../../src/test/GamMaSite.Tests/ApiFinanceControllerTests.cs) |
| GET | `/api/finance/admin/postings/years` | Admin postings and posting editor | `posteringer` | Returns all years present in posting dates for the year selector. | Not yet covered |
| GET | `/api/finance/admin/postings/options` | Admin posting editor | `account`, `postering_group` | Returns searchable account and posting-group options. | Not yet covered |
| GET | `/api/finance/admin/postings/{id}` | Admin posting editor | `posteringer` and source tables | Loads one posting, including source references and document information. | Not yet covered |
| POST | `/api/finance/admin/postings` | Admin posting editor | `posteringer` | Creates a manually maintained posting. The body is `FinanceAdminPostingUpdateDto` (`id`, `date`, `accountId`, `postingGroupId`, `userId`, `postingDate`, `text`, `amount`, `document`). | [FinanceReportServiceTests.cs](../../src/test/GamMaSite.Tests/FinanceReportServiceTests.cs) |
| PUT | `/api/finance/admin/postings/{id}` | Admin posting editor | `posteringer` | Updates a posting and its `updated_at` timestamp. | Not yet covered |
| POST | `/api/finance/admin/postings/{id}/duplicate` | Admin posting editor | `posteringer` | Creates a copy with the `-COPY` suffix. Existing IDs are rejected instead of overwritten. | Not yet covered |
| DELETE | `/api/finance/admin/postings/{id}` | Admin posting editor | `posteringer` | Deletes a posting. | Not yet covered |
| GET | `/api/finance/admin/import/history` | Admin CSV import page | `import_history` | Lists recent bank and MobilePay imports, including row counts, status, notes, and timestamps. | Not yet covered |
| POST | `/api/finance/admin/import/postings` | Admin CSV import page | `bank_account`, `mobilepay`, `posteringer` | Rebuilds missing derived postings from the already imported source data without uploading another CSV. Existing postings are skipped. | Not yet covered |
| GET | `/api/finance/admin/import/templates/{source}` | Admin CSV import page | No database; generated CSV | Downloads a UTF-8 example CSV for `bank` or `mobilepay`. Unknown sources return 404. | [ApiFinanceControllerTests.cs](../../src/test/GamMaSite.Tests/ApiFinanceControllerTests.cs) |
| POST | `/api/finance/admin/import` | Admin CSV import page | `bank_account`, `mobilepay`, `import_history`, optionally `posteringer` | Imports one or both CSV files. The multipart fields are `bankFile`, `mobilePayFile`, and `syncPostings`; the service validates and parses rows, upserts source data, writes import history, and optionally creates derived postings. | [ApiFinanceControllerTests.cs](../../src/test/GamMaSite.Tests/ApiFinanceControllerTests.cs), [FinanceImportServiceTests.cs](../../src/test/GamMaSite.Tests/FinanceImportServiceTests.cs) |
| POST | `/api/finance/admin/import/validate` | Admin CSV import page | No database; CSV parser only | Validates one or both selected CSV files without opening the Finance database or changing data. The multipart fields are `bankFile` and `mobilePayFile`; parser errors are returned before import. | [ApiFinanceControllerTests.cs](../../src/test/GamMaSite.Tests/ApiFinanceControllerTests.cs) |

Finance CRUD, PostgreSQL conflict handling, import-history persistence, bank/MobilePay matching, derived-posting generation, and read/write role permissions still require integration tests against a disposable PostgreSQL database. The current unit tests intentionally stop before opening a real Finance connection when validating input.

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
| POST | `/api/editor/images` | Admin event/news editor, admin message editor, admin template editor | `wwwroot/uploads/editor` file system | Validates and stores an uploaded editor image and returns its public URL. Requires `content.edit`, `email_templates.edit`, or `messages.edit`. | [ApiEditorControllerTests.cs](../../src/test/GamMaSite.Tests/ApiEditorControllerTests.cs) |

## Shared request behavior

All React API requests use same-origin credentials so the ASP.NET authentication cookie is included. `GET` requests do not require an antiforgery header. `POST`, `PUT`, and `DELETE` requests automatically obtain `/api/account/csrf-token` and send the returned token in `X-CSRF-TOKEN`, except for multipart image uploads, which use the same token through `FormData`.
