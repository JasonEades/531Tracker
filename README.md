<div align="center">

# 🏋️ 5/3/1 Tracker

**A modern, mobile-first workout tracker for Jim Wendler's 5/3/1 strength training program.**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-7B2FBE?style=for-the-badge&logo=blazor&logoColor=white)](https://blazor.net/)
[![MudBlazor](https://img.shields.io/badge/MudBlazor-9.x-594AE2?style=for-the-badge&logo=blazor&logoColor=white)](https://mudblazor.com/)
[![EF Core](https://img.shields.io/badge/EF_Core-10.0-68217A?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)](https://docs.microsoft.com/ef/core/)
[![SQLite](https://img.shields.io/badge/SQLite-Local-003B57?style=for-the-badge&logo=sqlite&logoColor=white)](https://sqlite.org/)
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)

<br/>

<img src="docs/screenshot.png" alt="5/3/1 Tracker Dashboard" width="340"/>

</div>

---

## 📖 What is 5/3/1?

**5/3/1** is a popular powerlifting and strength training program created by Jim Wendler. The program is built around four core barbell lifts:

| Lift | Description |
|---|---|
| 🟦 **Squat** | Lower body compound movement |
| 🟩 **Bench Press** | Upper body horizontal push |
| 🟥 **Deadlift** | Full-body posterior chain pull |
| 🟨 **Overhead Press** | Upper body vertical push |

Each lift follows a **3-week progression cycle** followed by a **deload week**, with percentages of your **Training Max (TM)** driving all weight calculations:

| Week | Sets × Reps | Percentages |
|---|---|---|
| **Week 1** — 5s | 3 × 5+ | 65% · 75% · **85%** |
| **Week 2** — 3s | 3 × 3+ | 70% · 80% · **90%** |
| **Week 3** — 5/3/1 | 3 × 5/3/1+ | 75% · 85% · **95%** |
| **Week 4** — Deload | 3 × 5 | 40% · 50% · 60% |

> The `+` means the final set is an **AMRAP** (as many reps as possible).

---

## ✨ Features

- 📊 **Dashboard** — at-a-glance view of your current cycle, today's workout, cycle completion progress, and all training maxes
- 🏃 **Workout Tracking** — step through each set, log completed reps, and mark workouts done
- 📅 **Cycle Management** — create new cycles, configure training maxes per lift, view full cycle history
- 🔁 **BBB (Boring But Big)** — optional 5×10 supplemental sets at a configurable percentage, supporting both *Same Day* and *Opposite Day* variants
- 📋 **First Set Last (FSL)** — optional 5×5 supplemental sets at the first main set's weight, the most popular supplemental template in 5/3/1 Forever
- 5️⃣ **5s PRO mode** — all main sets capped at straight 5 reps (no AMRAP), the standard Leader cycle protocol for building volume safely
- 🧮 **Estimated 1RM** — after completing an AMRAP set the app automatically calculates your estimated 1-rep max using the Epley formula and displays it in real time
- 🔥 **Warmup Sets** — optional auto-generated warmup sets at 40% and 50% of your training max
- 🏃 **Accessory Work** — log and track supplemental exercises alongside main lifts
- ⚖️ **Plate Calculator** — enter your available plates and bar weight; the app tells you exactly what to load on each side
- 🛠️ **Equipment Setup** — configure your bar weight and plate inventory (45s, 35s, 25s, 15s, 10s, 5s, 2.5s)
- 📱 **Mobile-First UI** — dark-themed, app-like interface designed for use in the gym from your phone

---

## 🖼️ Screenshots

<div align="center">

| Dashboard | Workout | Cycle Detail |
|:---:|:---:|:---:|
| Today's workout card, cycle progress, and training maxes | Step through sets with real-time plate loading | Full week-by-week breakdown of all lifts |

</div>

---

## 🏗️ Architecture

```
531Tracker/
├── Components/
│   ├── Layout/          # Shell layout, nav menu, theme
│   └── Pages/
│       ├── Home.razor          # Dashboard
│       ├── WorkoutDetail.razor # Active workout session
│       ├── CycleList.razor     # All cycles
│       ├── CycleDetail.razor   # Single cycle breakdown
│       ├── Lifts.razor         # Manage lifts & training maxes
│       ├── Accessories.razor   # Accessory exercise log
│       └── Equipment.razor     # Plate & bar configuration
├── Models/              # EF Core entity models
├── Services/            # Business logic (injected via DI)
│   ├── WeightCalculator        # % → weight math, set schemes
│   ├── PlateCalculatorService  # Plate loading algorithm
│   ├── CycleService            # Cycle CRUD & generation
│   ├── WorkoutService          # Workout session management
│   ├── LiftService             # Training max management
│   ├── BbbMappingService       # BBB lift pairing logic
│   └── AccessoryService        # Accessory tracking
├── Data/
│   └── AppDbContext.cs  # EF Core SQLite context
└── Migrations/          # EF Core migration history
```

**Tech stack:**

| Layer | Technology |
|---|---|
| Framework | [Blazor Server](https://learn.microsoft.com/aspnet/core/blazor/) on .NET 10 |
| UI Components | [MudBlazor](https://mudblazor.com/) v9 |
| ORM | [Entity Framework Core](https://docs.microsoft.com/ef/core/) 10 |
| Database | SQLite (local file — `fivethreeone.db`) |
| Styling | Custom CSS (dark theme, mobile-first) |

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2022/2026 **or** VS Code with the C# extension

### Run Locally

```bash
# Clone the repo
git clone https://github.com/JasonEades/531Tracker.git
cd 531Tracker

# Apply database migrations (creates fivethreeone.db automatically)
dotnet ef database update

# Run the app
dotnet run --project FiveThreeOneTracker.csproj
```

Then open your browser to `https://localhost:5001` (or the URL shown in the terminal).

### First-Time Setup

1. **Create a Cycle** — tap **+ Create Cycle** on the dashboard and enter your training maxes for each lift
2. *(Optional)* Enable **BBB** (Boring But Big) and choose Same Day or Opposite Day mode
3. *(Optional)* Enable **Warmup Sets** to auto-generate two warm-up sets before your main work
4. **Configure Equipment** — go to the Equipment page and enter your bar weight and available plates so the plate calculator works correctly
5. **Start Training** — tap **▶ Start Workout** on the dashboard and work through your sets

---

## 🔢 How Weight Calculations Work

All weights are derived from your **Training Max**, which is typically **90% of your true 1-rep max**.

```
Set Weight = round(Training Max × Set Percentage, nearest 5 lbs)
```

For example, with a **210 lb Bench Press TM** in Week 1:

| Set | % | Calculated | Rounded |
|---|---|---|---|
| Set 1 | 65% | 136.5 | **135 lbs** |
| Set 2 | 75% | 157.5 | **160 lbs** |
| Set 3 | 85% | 178.5 | **180 lbs** |

BBB and FSL sets use a separate configurable percentage (BBB default **50%**; FSL always uses the first main set weight).

### Estimated 1RM

After logging your AMRAP set the app shows your estimated 1-rep max using the **Epley formula**:

```
e1RM = round(weight × (1 + reps / 30), nearest 5 lbs)
```

Example: 180 lbs × 8 reps → **e1RM ≈ 230 lbs**

---

## 🤝 Contributing

Pull requests are welcome! For major changes please open an issue first to discuss what you'd like to change.

---

<div align="center">

Built with ❤️ and heavy iron by [@JasonEades](https://github.com/JasonEades)

</div>
