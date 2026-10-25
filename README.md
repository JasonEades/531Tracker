<div align="center">

# 🏋️ Jason's Garage Lab

**A mobile-first workout tracker for 5/3/1 and Push/Pull/Legs strength training.**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-7B2FBE?style=for-the-badge&logo=blazor&logoColor=white)](https://blazor.net/)
[![MudBlazor](https://img.shields.io/badge/MudBlazor-9.x-594AE2?style=for-the-badge&logo=blazor&logoColor=white)](https://mudblazor.com/)
[![EF Core](https://img.shields.io/badge/EF_Core-10.0-68217A?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)](https://learn.microsoft.com/ef/core/)
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)

**[Open the live site →](https://ppl531-jyi8z.ondigitalocean.app/)**

</div>

---

## 🌐 Live Site

The deployed application is available at:

**[https://ppl531-jyi8z.ondigitalocean.app/](https://ppl531-jyi8z.ondigitalocean.app/)**

Sign in with Google to create or access your private training data. The application is deployed as a multi-user service, so each account has isolated lifts, cycles, workouts, equipment, notes, and PPL programs.

---

## 📖 What is 5/3/1?

5/3/1 is a strength-training program built around four primary compound lifts:

| Lift | Movement |
|---|---|
| 🟦 **Squat** | Lower-body compound movement |
| 🟩 **Bench Press** | Upper-body horizontal push |
| 🟥 **Deadlift** | Posterior-chain pull |
| 🟨 **Overhead Press** | Upper-body vertical push |

A cycle contains three progressive training weeks followed by a deload week. Prescribed weights are calculated from each lift's Training Max (TM).

| Week | Main work | Percentages |
|---|---|---|
| **Week 1 — 5s** | 3 × 5+ | 65% · 75% · 85% |
| **Week 2 — 3s** | 3 × 3+ | 70% · 80% · 90% |
| **Week 3 — 5/3/1** | 5/3/1+ | 75% · 85% · 95% |
| **Week 4 — Deload** | 3 × 5 | 40% · 50% · 60% |

The `+` marks the final set as AMRAP (as many reps as possible). The optional 5s PRO mode uses straight sets instead of AMRAP work.

---

## ✨ Features

### 5/3/1 training

- Dashboard showing every active program (5/3/1 and PPL side by side) with direct open actions, so neither requires a trip to History
- Cycle creation and history across weeks and primary lifts
- Starting Training Maxes derive from estimated 1RMs (TM = 90% of e1RM): when you create your first 5/3/1 cycle and have previous PPL or 5/3/1 training, each lift's TM is set from the most recent program/cycle that trained it, replacing defaults or earlier edits; later cycles keep the normal increments and any manual overrides
- Main, warmup, BBB, FSL, and AMRAP set prescriptions
- Optional BBB (Boring But Big) work with Same Day and Opposite Day modes
- Optional FSL (First Set Last) 5×5 work
- Optional warmup sets
- Estimated 1RM using the Epley formula
- Accessory exercise tracking
- Plate-loading calculator based on available plates and bar weight
- Equipment configuration for bars and plate inventory

### Progress photos

Upload on **Body** with a drag-and-drop zone (tap to browse on mobile). Choosing a photo only selects it; nothing uploads until you set the date/caption and click **Save Photo**, after which the uploader clears. Photos are resized to 1280px, with 960px timeline thumbnails (`ProgressPhotos:ThumbnailDimension`) kept sharp on high-density screens; older, smaller thumbnails are upgraded automatically the first time they are viewed.

### Navigation feedback

A global progress bar and spinner appear the instant a link is tapped (and for programmatic navigation via Blazor's `enhancednavigationstart`/`enhancednavigationend` events), so slow mobile connections never look frozen. See `wwwroot/js/navigation-progress.js` and the `#nav-progress` element in `Components/App.razor`.

### Rest countdown

Both 5/3/1 and PPL rest timers play a short beep each second from 10 down to 1,
alongside the visual countdown. Tap or press a key in the app to enable browser
audio. Sound requires an active browser and audible device volume; the visual
timer remains available if audio is blocked or unsupported.
Turn off **Settings > Rest Timer > Rest countdown beeps** to mute both timers.
This preference defaults to on and is saved in the current browser.

### Publishing

Only the Admin account `jason.eades1@gmail.com` can access **Admin > Content** to
write, save drafts, edit, and publish training articles and product reviews.
The editor includes formatting tools, live Markdown preview, split view, and
fullscreen writing. Published articles are available at `/blog` and reviews at
`/reviews`; unpublished drafts remain private. Raw HTML and unsafe link
destinations are disabled. Posts and reviews support captioned photo galleries
with compact thumbnails that open in a full-size lightbox.
Public content pages include canonical URLs, search/social descriptions, and
Article or Review structured data. `/sitemap.xml` lists both indexes and every
published blog post and product review, along with the public fitness-tracker
overview; `/robots.txt` points crawlers to it and keeps private application
routes out of crawl discovery. Set `Seo__BaseUrl` to the public HTTPS origin
when deploying to a different domain.

The public `/fitness-tracker` page describes the app for search engines and visitors; training data stays behind sign-in.

### Additional primary-lift sets

Additional work can be recorded directly from a primary-lift workout without changing the programmed prescription. Each additional set supports:

- Weight and reps
- Set labels: Additional, Drop Set, Back-Off Set, Extra Volume, or Other
- Optional RPE and RIR
- Set-level notes
- Multiple sets through **Save & Add Another**
- Editing and deleting after creation
- Explicit entry ordering

Additional sets remain separate from programmed sets, stay attached to the historical workout, and are included in actual-work summaries and exports. They do not change Training Maxes, 5/3/1 percentages, FSL/BBB prescriptions, or planned work.

### Push/Pull/Legs

- Three-day or six-day splits
- Searchable exercise library with 81 curated strength exercises, muscle and equipment filters, exercise instructions, and account-private custom entries; PPL workout configuration selects catalog or custom exercises while retaining legacy labels and historical exercise-name snapshots
- Week-by-week workout configuration at `/ppl/{id}/workouts`: edit workout names, exercises, muscle groups, sets, and rep ranges per week; unconfigured weeks use the default schedule
- **Add Accessory Exercise** on any PPL day; accessories carry into generated sessions
- **⇄ Swap** any exercise in an unstarted PPL session for another catalog or custom exercise that trains the same muscle (same movement type listed first). Choose **This workout only** (the plan and its progression stay unchanged) or **All future workouts** (replaces the plan exercise). The swapped exercise's weight is derived from your e1RM history when available
- New programs derive starting weights from your previous training: each exercise uses the best e1RM from the most recent 5/3/1 cycle or PPL program that trained it (not the all-time best), falling back to manual entry when there is no history
- Exercise slots and session tracking; set weights (0–2000 lbs) and reps (0–100) are validated
- Weeks advance by completed rotations (e.g. every 3 sessions on a 3-day split), so missed days never skip workouts
- Double progression within configured rep ranges: when every set reaches the top of the range, the slot's weight increases by its exact configured increment (including 2.5 lb steps) on **Finish Session**, or earlier via the session's progression button; progression applies once per session and then shows the next-session weight
- Per-exercise estimated 1RM on the session page, plus estimated 1RM history for compound exercises on the dashboard
- Completed-session history and volume statistics, viewable for any program
- Switch the active PPL program from the Workouts page
- Program deletion asks for confirmation and removes all of the program's sessions

### Notes, history, and exports

- Workout day notes and exercise/set notes
- Persistent user-owned workout history
- Markdown and PDF exports for a single day, full week, or complete cycle
- Exported exercise sections clearly separate programmed sets from additional sets
- Export data includes actual weight, reps, types, RPE/RIR where available, notes, volume, and rep totals

### Authentication and privacy

- Google OAuth 2.0 sign-in
- Per-user data isolation: lifts, cycles, PPL programs and templates, accessories, bars/plates, default bar, and settings are all account-scoped; private accessories are owner-only
- Legacy unowned data is auto-claimed only on single-account installations
- Rest-beep preference is stored per account in the browser
- First-login provisioning of default lifts and equipment
- Admin user management at `/admin/users`
- Disabled accounts are blocked from access

---

## 🏗️ Architecture

```text
531Tracker/
├── Components/
│   ├── Layout/                 # Application shell, navigation, theme
│   └── Pages/                  # Blazor pages and workout flows
├── Models/                     # EF Core entities and enums
├── Services/
│   ├── WorkoutService          # Workout and additional-set persistence
│   ├── CycleService            # Cycle creation and workout generation
│   ├── WeightCalculator        # Training Max and set-scheme calculations
│   ├── PlateCalculatorService  # Plate-loading calculations
│   ├── AccessoryService        # Accessory tracking
│   ├── PplProgramService       # PPL program management
│   ├── PplSessionService       # PPL session tracking
│   ├── PplProgressionService   # PPL double progression and e1RM
│   ├── PplExerciseSwapService  # Same-muscle PPL exercise swaps (one-off or permanent)
│   ├── ExerciseLibraryService  # Curated and account-private exercise definitions
│   ├── EstimatedStrengthService # User-scoped e1RM used to seed 5/3/1 and PPL weights
│   ├── SiteContentSeo / SiteContentSitemap # Canonical URLs, metadata, sitemap.xml, robots.txt
│   ├── Export/                 # Normalized Markdown/PDF workout exports
│   ├── UserInitService         # First-login provisioning
│   └── AdminService            # User administration
├── Data/
│   └── AppDbContext.cs         # EF Core Identity database context
├── Migrations/                 # EF Core schema migrations
└── Tests/                      # xUnit persistence and export tests
```

### Technology stack

| Layer | Technology |
|---|---|
| Framework | Blazor Server on .NET 10 |
| UI components | MudBlazor 9 |
| ORM | Entity Framework Core 10 |
| Database | PostgreSQL in production or SQLite for local development |
| Authentication | ASP.NET Core Identity + Google OAuth 2.0 |
| PDF export | QuestPDF |
| Markdown export | Custom renderer with Markdig support |
| Styling | Custom dark, mobile-first CSS: design tokens in `wwwroot/app.css` (charcoal + safety-orange, Barlow / Barlow Condensed); plate colors shared via `PlateTone` + `.plate-tone-*` classes |

Authenticated pages prerender only their shell and loading state; data loads once the interactive circuit starts (`RendererInfo.IsInteractive`) to avoid querying twice per page view.

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2026 or VS Code with the C# extension
- Google Cloud OAuth credentials for local Google sign-in

### Clone and configure

```bash
git clone https://github.com/JasonEades/531Tracker.git
cd 531Tracker
```

Create `appsettings.Development.json` in the project root. The file is gitignored.

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    }
  },
  "App": {
    "AdminEmail": "your@gmail.com"
  }
}
```

In Google Cloud Console, create a web OAuth client and add the local callback URL shown by the application, commonly:

```text
https://localhost:PORT/signin-google
```

### Run locally

```bash
dotnet restore
dotnet ef database update
dotnet run --project FiveThreeOneTracker.csproj
```

Open the URL shown in the terminal, then sign in with Google. Migrations are also applied automatically during application startup in the configured environment.

### Configure Google Health

Google Health uses a second authorization grant for the already signed-in Google user. It does not create another application account and it does not use Google Fit, Android, or Health Connect.

Before connecting a user, complete the following in Google Cloud:

1. Use the Google Cloud project that owns the existing web OAuth client, or create a dedicated web OAuth client in the project used by this application.
2. Enable the current Google Health API used by the deployment and confirm that the activity/fitness REST resources are available to the project.
3. Configure the OAuth consent screen with the application name, support email, authorized domain, privacy-policy URL, and contact information.
4. Add the development Google accounts as test users while the consent screen is in testing.
5. Request these scopes:

   ```text
   openid
   email
   https://www.googleapis.com/auth/googlehealth.activity_and_fitness.readonly
   ```

6. Add the exact Health callback URI to the web OAuth client. Examples:

   ```text
   https://localhost:PORT/health/google/callback
   https://ppl531-jyi8z.ondigitalocean.app/health/google/callback
   ```

   The callback is different from the normal sign-in callback `/signin-google`.
7. The application calls the current daily steps endpoint directly with a JSON POST body:

   ```text
   https://health.googleapis.com/v4/users/me/dataTypes/steps/dataPoints:dailyRollUp
   ```

   It sends the requested civil date window as nested `range.start.date` and `range.end.date` values, with a one-day `windowSizeDays` and `pageSize` of 90, and reads each day's `rollupDataPoints[].steps.countSum` value.
8. If Google classifies the requested scope as sensitive or restricted, complete Google's verification process before production use. Provide the requested privacy disclosures and demonstrate that tokens remain server-side.

For local development, do not put secrets in `appsettings.json` or commit them to `appsettings.Development.json`. Use user secrets:

```powershell
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_WEB_CLIENT_ID" --project FiveThreeOneTracker.csproj
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_WEB_CLIENT_SECRET" --project FiveThreeOneTracker.csproj
```

The Health client ID and secret fall back to the existing `Authentication:Google` values. They can be overridden independently when Google requires a separate web OAuth client:

```powershell
dotnet user-secrets set "Authentication:GoogleHealth:ClientId" "YOUR_HEALTH_WEB_CLIENT_ID" --project FiveThreeOneTracker.csproj
dotnet user-secrets set "Authentication:GoogleHealth:ClientSecret" "YOUR_HEALTH_WEB_CLIENT_SECRET" --project FiveThreeOneTracker.csproj
```

For DigitalOcean App Platform, define the equivalent encrypted environment variables using double underscores:

```text
Authentication__Google__ClientId
Authentication__Google__ClientSecret
Authentication__GoogleHealth__ClientId       (optional override)
Authentication__GoogleHealth__ClientSecret   (optional override)
```

After changing OAuth credentials, rotate any previously exposed client secret in Google Cloud. Never log or expose `ClientSecret`, access tokens, or refresh tokens. The application encrypts stored Health tokens with ASP.NET Core Data Protection and persists the protection keys in the database.

To verify configuration locally:

```powershell
dotnet build
dotnet test Tests/WorkoutExportTests.csproj
dotnet run --project FiveThreeOneTracker.csproj
```

Sign in normally first, open **Settings → Google Health**, select **Connect Google Health**, approve the read-only activity scope, and then use **Sync Now**. The callback must return to the same authenticated browser session. A successful sync stores daily step records and represents dates covered by a training cycle as Cardio → Steps.

### Database providers

The application supports both PostgreSQL and SQLite:

- **PostgreSQL** is used when `DATABASE_URL` is set, or when a `ConnectionStrings:Postgres` value is configured. This is the recommended configuration for hosted deployments.
- **SQLite** is the local-development fallback and stores data in `fivethreeone.db` when no PostgreSQL connection is configured.

For local PostgreSQL development, configure the connection string as an environment variable or user secret:

```powershell
$env:DATABASE_URL = "Host=localhost;Port=5432;Database=fivethreeone;Username=postgres;Password=your-password"
```

The application also accepts PostgreSQL URLs in the form `postgresql://user:password@host:port/database` and converts them automatically. This format is supported by many hosting providers.

### First-time setup

1. Sign in with Google; default lifts and equipment are provisioned automatically.
2. Create a cycle from the dashboard and configure Training Maxes.
3. Optionally enable BBB, FSL, 5s PRO, and warmup sets.
4. Configure the bar and plate inventory on the Equipment page.
5. Start a workout and record programmed and additional primary-lift work.

---

## 🔢 Weight calculations

Prescribed weights use the following calculation and are rounded to the nearest 5 lb by the application:

```text
Set Weight = round(Training Max × Set Percentage, nearest 5 lb)
```

For a 210 lb Bench Press Training Max in Week 1:

| Set | Percentage | Raw weight | Rounded weight |
|---|---:|---:|---:|
| Set 1 | 65% | 136.5 | 135 lb |
| Set 2 | 75% | 157.5 | 160 lb |
| Set 3 | 85% | 178.5 | 180 lb |

BBB uses its configured percentage, while FSL uses the first main-set weight. Additional sets use the actual weight and reps entered by the user and do not modify future programming.

### Estimated 1RM

For eligible completed AMRAP work, the app uses the Epley formula:

```text
e1RM = round(weight × (1 + reps / 30), nearest 5 lb)
```

Example: 180 lb × 8 reps produces an estimated 1RM of approximately 230 lb.

When seeding a first 5/3/1 cycle or a new PPL program, estimates come from the **previous** cycle/program: for each lift, the most recently trained 5/3/1 cycle or PPL program (by workout date) that contains completed sets for it, using that source's best e1RM.

---

## 🧪 Testing

The test project uses xUnit and covers the critical workout/export paths, including:

- Additional-set creation and persistence
- Multiple-set ordering
- Editing and deleting additional sets
- Preservation of programmed sets
- Validation of weight, reps, and RPE values
- Primary-lift-only restrictions
- Markdown separation and formatting
- PDF generation with additional-set data
- Workout note persistence and export behavior
- PPL weekly templates, accessory persistence, and estimated-1RM weight derivation
- PPL program deletion with existing sessions and cross-user delete isolation
- First-cycle/program weight seeding from the most recent previous 5/3/1 cycle or PPL program
- PPL progression (once-only, 2.5 lb increments, auto-apply on finish, user isolation), rotation-based week numbering, and set-value validation
- PPL exercise swaps: same-muscle options, one-off vs. permanent scope, no progression of the plan exercise after a one-off swap, rejection after sets are logged, and cross-user isolation
- Exercise-library search/filtering, custom-exercise ownership isolation, and PPL catalog selection
- Multi-user isolation of settings, bars, programs, accessories, and the dashboard's current-cycle lookup
- Sitemap, robots.txt, and SEO metadata for published content

Run the full solution build and focused tests with:

```bash
dotnet build
dotnet test Tests/WorkoutExportTests.csproj
```

---

## 🛡️ Admin panel

The configured `App:AdminEmail` is granted the `Admin` role during startup. Administrators can:

- View registered users at `/admin/users`
- Enable or disable non-admin accounts
- Prevent disabled accounts from signing in

The admin navigation link is shown only to administrators.

---

## 🤝 Contributing

Pull requests are welcome. For substantial changes, open an issue first to discuss the proposed design and behavior.

<div align="center">

Built with ❤️ and heavy iron by [@JasonEades](https://github.com/JasonEades)

</div>
