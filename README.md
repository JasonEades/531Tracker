<div align="center">

# 🏋️ The Lifting Lab

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

- Dashboard with current cycle, next workout, completion progress, and Training Maxes
- Cycle creation and history across weeks and primary lifts
- Main, warmup, BBB, FSL, and AMRAP set prescriptions
- Optional BBB (Boring But Big) work with Same Day and Opposite Day modes
- Optional FSL (First Set Last) 5×5 work
- Optional warmup sets
- Estimated 1RM using the Epley formula
- Accessory exercise tracking
- Plate-loading calculator based on available plates and bar weight
- Equipment configuration for bars and plate inventory

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
- Exercise slots and session tracking
- Double progression within configured rep ranges
- Estimated 1RM history for compound exercises
- Optional synchronization of PPL progress to 5/3/1 Training Maxes
- Completed-session history and volume statistics

### Notes, history, and exports

- Workout day notes and exercise/set notes
- Persistent user-owned workout history
- Markdown and PDF exports for a single day, full week, or complete cycle
- Exported exercise sections clearly separate programmed sets from additional sets
- Export data includes actual weight, reps, types, RPE/RIR where available, notes, volume, and rep totals

### Authentication and privacy

- Google OAuth 2.0 sign-in
- Per-user data isolation
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
│   ├── PplProgressionService   # PPL progression and TM sync
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
| Styling | Custom dark, mobile-first CSS |

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
