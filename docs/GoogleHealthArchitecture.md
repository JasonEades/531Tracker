# Google Health Integration Architecture Audit

## Current authentication

- The application is a Blazor Server app targeting .NET 10.
- ASP.NET Core Identity with EF Core (`IdentityDbContext<ApplicationUser>`) owns application accounts and issues the application cookie.
- Google login uses `Microsoft.AspNetCore.Authentication.Google` via `AddGoogle`, with callback `/signin-google`.
- The Google login callback reads email, display name, picture, and `ClaimTypes.NameIdentifier`.
- New users are created with `UserName`/`Email` from the Google email and receive an Identity external login with provider `Google` and provider key equal to Google's subject identifier. Existing users are currently looked up by email; the callback does not currently verify that a subsequent Google subject matches the stored external login.
- Server-side ownership is normally derived from the authenticated Identity cookie's `ClaimTypes.NameIdentifier`, which is the Identity `ApplicationUser.Id`, through `ICurrentUserService`. Health operations must use this service and never accept a client user ID.

## Existing OAuth configuration and tokens

- A Google OAuth client ID and secret are read from `Authentication:Google:ClientId` and `Authentication:Google:ClientSecret`.
- The existing login configuration is suitable for the same Google Cloud project/client only if its consent-screen and redirect configuration support the additional authorization flow. The login callback must remain unchanged for ordinary sign-in.
- Existing login uses the normal Google OpenID Connect/profile scopes supplied by the authentication handler. It does not request `googlehealth.activity_and_fitness.readonly`.
- No Google access token or refresh token is currently persisted. The login ticket is used only to create/sign in the Identity user.
- Therefore, the login token cannot be assumed to authorize Health API calls. Health requires a separate authorization-code flow with the minimum Health scope and offline access.

## Planned Health authorization

- An authenticated user selects **Connect Google Health**.
- The server creates a protected, short-lived OAuth state containing the return URL and current Identity user ID, then redirects to Google's authorization endpoint with `access_type=offline`, consent as needed, and only `https://www.googleapis.com/auth/googlehealth.activity_and_fitness.readonly`.
- The callback validates state, requires the same authenticated application user, exchanges the code server-side, and verifies the returned Google account subject/profile identity against the application's Google external-login association. A mismatch fails closed and never links the Health grant to another account.
- Access and refresh tokens are stored only server-side. Token values are encrypted with ASP.NET Core Data Protection before persistence; refresh tokens are never rendered, logged, or sent to the browser. Revocation clears/invalidates the local connection and requires reconnecting.
- The application's Identity user ID remains the primary ownership key; Google's stable subject is stored as a provider identifier for consistency checks.

## Data model and invariants

- `GoogleHealthConnection`: one per application user/provider, encrypted token fields, Google subject, granted scopes, expiry, connection status, and synchronization metadata.
- `DailyStepRecord`: one row per application user/provider/metric/local calendar date, storing the imported count and source metadata. This is the health source of truth.
- Existing `AdditionalSession` with `SessionType.Cardio` and `CardioEntry` remains the training-history representation. `CardioEntry` gains provider/source metadata and a stable imported-record relationship so exactly one Google Health step entry exists per user/date.
- The canonical Cardio accessory is `Steps` under `AccessoryCategory.Cardio`, with `CardioUnit.Steps`; no strength fields are involved.
- Unique indexes enforce user/provider/metric/date idempotency. Updates to a daily record update its linked cardio entry rather than inserting another entry.

## Cycle/week mapping

- Current cycles generate workout dates from `Cycle.CreatedAt`, but `Cycle` has no durable start date and `Week` has no date range. Add a persisted cycle start date and derive its 28-day end date; each week is the seven-day interval beginning at `Cycle.StartDate + (week number - 1) * 7`.
- Existing cycles are backfilled from their earliest generated workout date, with deterministic fallback to `CreatedAt.Date` when no workout date exists. New cycles persist their start date at creation.
- Synchronization resolves each record using its actual local health date: find the user's cycle whose date range contains it, then the week whose seven-day range contains it. It never uses the current cycle or synchronization date.
- Records outside all cycles remain preserved and unassigned. A reconciliation service can assign them if a later cycle is created or edited. Overlapping cycles use a deterministic rule documented in the service and tests.

## Synchronization and background work

- `IGoogleHealthApiClient` encapsulates token refresh, HTTPS REST calls, serialization, retry/rate limiting, and non-sensitive error logging.
- `IHealthSyncService` performs an initial historical import, incremental overlap imports, and manual sync. Each date is upserted in a transaction, followed by cardio upsert and cycle/week reconciliation.
- A server-side hosted scheduler enqueues/executes per-user sync work with a lease/lock so repeated jobs are safe. The webhook option is deferred until the enabled API contract is confirmed; notifications, if supported, will enqueue work rather than perform long requests.
- Analytics query `DailyStepRecord` as canonical data and join cardio only for training-history display, preventing double counting. Dashboard, weekly/monthly/yearly analytics, correlations, and exports use the same canonical record.

## UI, editing, and deletion behavior

- Add an authenticated Health Integrations settings section with Connect, Sync Now, Reconnect, Disconnect, connection state, last sync, and scope explanation.
- Imported entries are visibly marked `Source: Google Health`; manual cardio remains unchanged. Imported values are not manually editable by default.
- Removing a local imported representation must not delete Google data; the next sync may recreate it. The underlying record remains the canonical source unless the user disconnects and explicitly clears local data according to the final UI policy.
- Steps enrich additional activity and do not affect programmed workout completion, progression, training maxes, or cycle advancement.

## Google Cloud configuration and verification

- Enable the current Google Health API in the Google Cloud project associated with the OAuth client.
- Configure the OAuth consent screen, the Health read scope, development test users, authorized domain, and HTTPS redirect URI for the Health callback (separate from `/signin-google`).
- Keep client secrets in ASP.NET Core user secrets, deployment secrets, or environment variables; never commit them. Production access may require Google verification for the sensitive/restricted scope and published privacy/usage disclosures.
- No Android application, Health Connect component, MAUI project, Google Fit API, or Fitbit integration is part of this design.
