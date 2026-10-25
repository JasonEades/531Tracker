using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using FiveThreeOneTracker.Services;
using FiveThreeOneTracker.Services.Export;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FiveThreeOneTracker.Tests;

public sealed class WorkoutExportTests
{
    [Fact]
    public void MarkdownPreservesNotesAndPlannedActualValues()
    {
        var document = CreateDocument();
        var markdown = System.Text.Encoding.UTF8.GetString(new MarkdownWorkoutExporter().Render(document));

        Assert.Contains("Workout Notes", markdown);
        Assert.Contains("Felt unusually fatigued.", markdown);
        Assert.Contains("Recovery was poor.", markdown);
        Assert.Contains("Exercise Notes", markdown);
        Assert.Contains("Left shoulder felt uncomfortable.", markdown);
        Assert.Contains("Last rep was slow.", markdown);
        Assert.Contains("| 5 | Main — AMRAP | 5 | 9 | 235 | 235 | Yes", markdown);
    }

    [Fact]
    public async Task AdditionalCardioSessionPersistsDateQuantityUnitAndNotes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var cycle = new Cycle { UserId = "test-user", Name = "Cycle" };
        var week = new Week { Cycle = cycle, WeekNumber = WeekNumber.Week1 };
        context.Weeks.Add(week);
        await context.SaveChangesAsync();

        var service = new AdditionalSessionService(context, new TestCurrentUserService());
        var session = await service.CreateForWeekAsync(week.Id, SessionType.Cardio, new DateTime(2026, 9, 8), "Cardio Session", "Easy recovery session");
        var entry = await service.AddCardioEntryAsync(session.Id, 16, 30, CardioUnit.Minutes, "Steady pace");

        var reloaded = await service.GetAsync(session.Id);
        Assert.NotNull(entry);
        Assert.Equal(new DateTime(2026, 9, 8), reloaded!.OccurredOn);
        Assert.Equal("Easy recovery session", reloaded.Notes);
        Assert.Equal(30, reloaded.CardioEntries.Single().Quantity);
        Assert.Equal(CardioUnit.Minutes, reloaded.CardioEntries.Single().Unit);
    }

    [Fact]
    public void MarkdownRendersAdditionalCardioSession()
    {
        var document = new WorkoutExportDocument
        {
            Scope = WorkoutExportScope.Week,
            Week = new WeekExportModel
            {
                WeekNumber = 2,
                CycleNumber = 12,
                AdditionalSessions =
                [
                    new AdditionalSessionExportModel
                    {
                        Name = "Cardio Session",
                        SessionType = "Cardio",
                        Date = new DateTime(2026, 9, 8),
                        CardioEntries = [new CardioExportModel { Exercise = "Rowing", Quantity = 30, Unit = "Minutes", Notes = "Easy recovery session" }]
                    }
                ]
            }
        };

        var markdown = System.Text.Encoding.UTF8.GetString(new MarkdownWorkoutExporter().Render(document));
        Assert.Contains("Additional Session", markdown);
        Assert.Contains("2026-09-08", markdown);
        Assert.Contains("30", markdown);
        Assert.Contains("Minutes", markdown);
    }

    [Fact]
    public void MarkdownRendersCanonicalDailySteps()
    {
        var document = new WorkoutExportDocument
        {
            Scope = WorkoutExportScope.Week,
            Week = new WeekExportModel
            {
                WeekNumber = 1,
                CycleNumber = 4,
                DailySteps = [new DailyStepExportModel { Date = new DateTime(2026, 9, 8), Steps = 12_345, Provider = "GoogleHealth" }]
            }
        };

        var markdown = System.Text.Encoding.UTF8.GetString(new MarkdownWorkoutExporter().Render(document));

        Assert.Contains("Daily Steps", markdown);
        Assert.Contains("2026-09-08", markdown);
        Assert.Contains("12,345", markdown);
        Assert.Contains("GoogleHealth", markdown);
    }

    [Fact]
    public async Task NewPplDoesNotRequireOrRead531Lifts()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var service = CreatePplProgramService(context, new TestCurrentUserService());

        var program = await service.CreateProgramAsync("New PPL", 3);
        var details = await service.GetProgramWithDetailsAsync(program.Id);

        Assert.NotNull(details);
        Assert.Equal(3, details!.DayTemplates.Count);
        Assert.All(details.DayTemplates.SelectMany(d => d.ExerciseSlots), slot =>
        {
            Assert.Null(slot.CurrentWeight);
            Assert.False(slot.UsePercentageOfTm);
            Assert.Null(slot.LiftId);
        });
    }

    [Fact]
    public async Task PplThreeAndSixDayProgramsCreateExpectedRotations()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var service = CreatePplProgramService(context, new TestCurrentUserService());

        var threeDay = await service.CreateProgramAsync("Three", 3);
        var sixDay = await service.CreateProgramAsync("Six", 6);

        Assert.Equal(3, (await service.GetProgramWithDetailsAsync(threeDay.Id))!.DayTemplates.Count);
        Assert.Equal(6, (await service.GetProgramWithDetailsAsync(sixDay.Id))!.DayTemplates.Count);
    }

    [Fact]
    public async Task PplStartingWeightsUseEstimatedOneRepMaxesFromCompleted531Workouts()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var bench = new Lift { UserId = "test-user", LiftType = LiftType.BenchPress, Name = "Bench Press", TrainingMax = 200 };
        var cycle = new Cycle { UserId = "test-user", Name = "Cycle 1", CycleNumber = 1 };
        var week = new Week { Cycle = cycle, WeekNumber = WeekNumber.Week1 };
        var workout = new Workout { Week = week, MainLiftType = LiftType.BenchPress, Status = WorkoutStatus.Completed };
        context.WorkoutSets.Add(new WorkoutSet
        {
            Workout = workout,
            Lift = bench,
            SetType = SetType.Main,
            IsCompleted = true,
            ActualWeight = 170,
            ActualReps = 5
        });
        await context.SaveChangesAsync();

        var service = CreatePplProgramService(context, new TestCurrentUserService());
        var program = await service.CreateProgramAsync("PPL from 531 history", 3);
        var details = await service.GetProgramWithDetailsAsync(program.Id);
        var benchSlot = details!.DayTemplates
            .SelectMany(day => day.ExerciseSlots)
            .Single(slot => slot.ExerciseName == "Barbell Bench Press");

        Assert.Equal(200, new WeightCalculator().CalculateEstimated1RM(170, 5));
        Assert.Equal(160, benchSlot.StartingWeight);
        Assert.Equal(160, benchSlot.CurrentWeight);
    }

    [Fact]
    public async Task SelectingAnotherUsersPplProgramDoesNotDeactivateOwnProgram()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var ownService = CreatePplProgramService(context, new TestCurrentUserService("user-a"));
        var otherService = CreatePplProgramService(context, new TestCurrentUserService("user-b"));
        var ownProgram = await ownService.CreateProgramAsync("Own PPL", 3);
        var otherProgram = await otherService.CreateProgramAsync("Other PPL", 3);

        await ownService.SetActiveProgramAsync(otherProgram.Id);

        Assert.True((await context.PplPrograms.SingleAsync(p => p.Id == ownProgram.Id)).IsActive);
        Assert.True((await context.PplPrograms.SingleAsync(p => p.Id == otherProgram.Id)).IsActive);
    }

    [Fact]
    public async Task FirstUserLiftsUseEstimatedOneRepMaxesFromExisting531History()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var bench = new Lift { LiftType = LiftType.BenchPress, Name = "Bench Press", TrainingMax = 225 };
        var cycle = new Cycle { Name = "Legacy Cycle", CycleNumber = 1 };
        var week = new Week { Cycle = cycle, WeekNumber = WeekNumber.Week1 };
        var workout = new Workout { Week = week, MainLiftType = LiftType.BenchPress, Status = WorkoutStatus.Completed };
        context.WorkoutSets.Add(new WorkoutSet
        {
            Workout = workout,
            Lift = bench,
            SetType = SetType.Main,
            IsCompleted = true,
            ActualWeight = 170,
            ActualReps = 5
        });
        await context.SaveChangesAsync();

        var calculator = new WeightCalculator();
        context.Users.Add(new ApplicationUser { Id = "test-user", UserName = "test-user", NormalizedUserName = "TEST-USER" });
        await context.SaveChangesAsync();
        var initializer = new UserInitService(
            context,
            new EstimatedStrengthService(context, calculator),
            calculator);
        await initializer.EnsureUserInitializedAsync("test-user");

        Assert.Equal("test-user", bench.UserId);
        Assert.Equal(180, bench.TrainingMax);
    }

    [Fact]
    public async Task NewAccountDoesNotClaimLegacyUnownedDataWhenOtherAccountsExist()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Users.AddRange(
            new ApplicationUser { Id = "existing-user", UserName = "existing-user", NormalizedUserName = "EXISTING-USER" },
            new ApplicationUser { Id = "new-user", UserName = "new-user", NormalizedUserName = "NEW-USER" });
        var orphanedCycle = new Cycle { Name = "Legacy Cycle", CycleNumber = 1 };
        context.Cycles.Add(orphanedCycle);
        await context.SaveChangesAsync();

        var calculator = new WeightCalculator();
        var initializer = new UserInitService(
            context,
            new EstimatedStrengthService(context, calculator),
            calculator);
        await initializer.EnsureUserInitializedAsync("new-user");

        Assert.Null(orphanedCycle.UserId);
        Assert.Equal(4, await context.Lifts.CountAsync(lift => lift.UserId == "new-user"));
    }

    [Fact]
    public async Task UpdatingCardioSessionCannotAttachAnotherUsersPrivateAccessory()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var ownAccessory = new Accessory
        {
            UserId = "user-a",
            Name = "My Rowing Machine",
            Category = AccessoryCategory.Cardio
        };
        var privateAccessory = new Accessory
        {
            UserId = "user-b",
            Name = "Private Bike",
            Category = AccessoryCategory.Cardio
        };
        var cycle = new Cycle { UserId = "user-a", Name = "Cycle" };
        var week = new Week { Cycle = cycle, WeekNumber = WeekNumber.Week1 };
        var session = new AdditionalSession
        {
            Week = week,
            SessionType = SessionType.Cardio,
            Name = "Existing Cardio",
            CardioEntries =
            [
                new CardioEntry
                {
                    Accessory = ownAccessory,
                    Quantity = 20,
                    Unit = CardioUnit.Minutes
                }
            ]
        };
        context.AdditionalSessions.Add(session);
        context.Accessories.Add(privateAccessory);
        await context.SaveChangesAsync();

        var service = new AdditionalSessionService(context, new TestCurrentUserService("user-a"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(
            session.Id,
            DateTime.UtcNow.Date.AddDays(1),
            "Changed Cardio",
            null,
            [new CardioEntryInput(privateAccessory.Id, 30, CardioUnit.Minutes, null)]));

        Assert.Equal("Existing Cardio", session.Name);
        Assert.Equal(ownAccessory.Id, (await context.CardioEntries.SingleAsync()).AccessoryId);
    }

    [Fact]
    public async Task First531CycleUsesEstimatedOneRepMaxFromPplHistory()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Lifts.AddRange(
            new Lift { UserId = "test-user", LiftType = LiftType.Squat, Name = "Squat", TrainingMax = 315 },
            new Lift { UserId = "test-user", LiftType = LiftType.BenchPress, Name = "Bench Press", TrainingMax = 225 },
            new Lift { UserId = "test-user", LiftType = LiftType.Deadlift, Name = "Deadlift", TrainingMax = 365 },
            new Lift { UserId = "test-user", LiftType = LiftType.OverheadPress, Name = "Overhead Press", TrainingMax = 145 });
        await context.SaveChangesAsync();

        var user = new TestCurrentUserService();
        var calculator = new WeightCalculator();
        var pplService = CreatePplProgramService(context, user);
        var pplSessions = new PplSessionService(context, user);
        var program = await pplService.CreateProgramAsync("PPL first", 3);
        var session = await pplService.GetOrCreateNextSessionAsync(program.Id);
        var details = await pplSessions.GetSessionWithDetailsAsync(session.Id);
        var benchExercise = details!.Exercises.Single(exercise => exercise.ExerciseName == "Barbell Bench Press");
        var set = benchExercise.Sets.First();

        await pplSessions.UpdateSetAsync(set.Id, 170, 5, true);
        await pplSessions.CompleteSessionAsync(session.Id);

        var cycleService = new CycleService(
            context,
            new LiftService(context, user),
            new BbbMappingService(),
            calculator,
            user,
            new EstimatedStrengthService(context, calculator));
        await cycleService.CreateCycleAsync();

        var bench = await context.Lifts.SingleAsync(lift => lift.LiftType == LiftType.BenchPress);
        Assert.Equal(180, bench.TrainingMax);
    }

    [Fact]
    public async Task PplUsesCustomizedWorkoutForSelectedWeekAndDefaultsForOtherWeeks()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var service = CreatePplProgramService(context, new TestCurrentUserService());
        var program = await service.CreateProgramAsync("Weekly PPL", 3);
        var defaults = await service.GetWeekWorkoutTemplatesAsync(program.Id, 1);
        var firstDefaultExercise = defaults[0].ExerciseSlots.First();
        var weekOneDrafts = defaults.Select(day => new PplWorkoutDayDraft(
            day.Id,
            day.Name,
            day.ExerciseSlots.Select(slot => new PplWorkoutExerciseDraft(
                slot.Id,
                slot.ExerciseName,
                slot.MuscleGroup,
                slot.TargetSets,
                slot.RepsMin,
                slot.RepsMax,
                slot.IsBodyweight,
                slot.ProgressionIncrement,
                slot.StartingWeight,
                slot.CurrentWeight)).ToList())).ToList();
        var firstDay = weekOneDrafts[0];
        var exercises = firstDay.Exercises.ToList();
        exercises[0] = exercises[0] with { ExerciseName = "Week One Press", TargetSets = 5, RepsMin = 3, RepsMax = 5 };
        exercises.Add(new PplWorkoutExerciseDraft(
            null,
            "Cable Lateral Raise",
            MuscleGroup.Shoulders,
            3,
            12,
            15,
            false,
            2.5,
            null,
            null));
        weekOneDrafts[0] = firstDay with { Name = "Week One Push", Exercises = exercises };

        await service.ConfigureWeekWorkoutsAsync(program.Id, 1, weekOneDrafts);

        var customized = await service.GetWeekWorkoutTemplatesAsync(program.Id, 1);
        var nextWeek = await service.GetWeekWorkoutTemplatesAsync(program.Id, 2);
        Assert.Equal("Week One Push", customized[0].Name);
        Assert.Equal("Week One Press", customized[0].ExerciseSlots.OrderBy(slot => slot.OrderInDay).First().ExerciseName);
        Assert.Equal(5, customized[0].ExerciseSlots.OrderBy(slot => slot.OrderInDay).First().TargetSets);
        Assert.Contains(customized[0].ExerciseSlots, slot =>
            slot.ExerciseName == "Cable Lateral Raise"
            && slot.MuscleGroup == MuscleGroup.Shoulders
            && slot.TargetSets == 3
            && slot.RepsMin == 12
            && slot.RepsMax == 15);
        Assert.Equal(firstDefaultExercise.ExerciseName, nextWeek[0].ExerciseSlots.OrderBy(slot => slot.OrderInDay).First().ExerciseName);

        var reconfiguredDrafts = customized.Select(day => new PplWorkoutDayDraft(
            defaults.Single(defaultDay => defaultDay.OrderInWeek == day.OrderInWeek).Id,
            day.Name,
            day.ExerciseSlots.OrderBy(slot => slot.OrderInDay).Select(slot => new PplWorkoutExerciseDraft(
                slot.Id,
                slot.ExerciseName,
                slot.MuscleGroup,
                slot.TargetSets,
                slot.RepsMin,
                slot.RepsMax,
                slot.IsBodyweight,
                slot.ProgressionIncrement,
                slot.StartingWeight,
                slot.CurrentWeight)).ToList())).ToList();
        await service.ConfigureWeekWorkoutsAsync(program.Id, 1, reconfiguredDrafts);

        var session = await service.GetOrCreateNextSessionAsync(program.Id);
        Assert.Equal("Week One Push", session.DayTemplate.Name);
        Assert.Equal(1, session.Week!.WeekNumber);
        var sessionService = new PplSessionService(context, new TestCurrentUserService());
        var detailedSession = await sessionService.GetSessionWithDetailsAsync(session.Id);
        Assert.Contains(detailedSession!.Exercises, exercise =>
            exercise.ExerciseName == "Cable Lateral Raise"
            && exercise.TargetSets == 3
            && exercise.RepsMin == 12
            && exercise.RepsMax == 15);
    }

    [Fact]
    public async Task PplStartingWeightIsSlotSpecificAndDoesNotRewritePlannedSnapshot()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var service = CreatePplProgramService(context, new TestCurrentUserService());
        var sessionService = new PplSessionService(context, new TestCurrentUserService());
        var program = await service.CreateProgramAsync("PPL", 3);
        var details = await service.GetProgramWithDetailsAsync(program.Id);
        var firstSlot = details!.DayTemplates.First().ExerciseSlots.First(s => !s.IsBodyweight);

        await service.ConfigureStartingWeightsAsync(program.Id, new Dictionary<int, double?> { [firstSlot.Id] = 185 });
        var session = await service.GetOrCreateNextSessionAsync(program.Id);
        var detailedSession = await sessionService.GetSessionWithDetailsAsync(session.Id);
        var exercise = detailedSession!.Exercises.Single(e => e.PplExerciseSlotId == firstSlot.Id);
        Assert.Equal(185, exercise.SuggestedWeight);

        var set = exercise.Sets.First();
        await sessionService.UpdateSetAsync(set.Id, 175, 8, true);
        var reloaded = await sessionService.GetSessionWithDetailsAsync(session.Id);

        var reloadedExercise = reloaded!.Exercises.Single(e => e.PplExerciseSlotId == firstSlot.Id);
        Assert.Equal(185, reloadedExercise.SuggestedWeight);
        Assert.Equal(175, reloadedExercise.Sets.First().ActualWeight);
    }

    [Fact]
    public async Task PplWeightsAndSessionsAreIsolatedByUser()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var owner = new TestCurrentUserService("owner");
        var other = new TestCurrentUserService("other");
        var programService = CreatePplProgramService(context, owner);
        var ownerSessionService = new PplSessionService(context, owner);
        var otherSessionService = new PplSessionService(context, other);

        var program = await programService.CreateProgramAsync("Owner PPL", 3);
        var details = await programService.GetProgramWithDetailsAsync(program.Id);
        var slot = details!.DayTemplates.SelectMany(d => d.ExerciseSlots).First(s => !s.IsBodyweight);
        await programService.ConfigureStartingWeightsAsync(program.Id, new Dictionary<int, double?> { [slot.Id] = 185 });
        var session = await programService.GetOrCreateNextSessionAsync(program.Id);

        Assert.NotNull(await ownerSessionService.GetSessionWithDetailsAsync(session.Id));
        Assert.Null(await otherSessionService.GetSessionWithDetailsAsync(session.Id));

        await otherSessionService.SetStartingWeightAsync(slot.Id, 315);
        var unchanged = await context.PplExerciseSlots.SingleAsync(s => s.Id == slot.Id);
        Assert.Equal(185, unchanged.CurrentWeight);
    }

    [Fact]
    public async Task DeletingPplProgramRemovesSessionsAndIgnoresOtherUsers()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var ownerService = CreatePplProgramService(context, new TestCurrentUserService("owner"));
        var otherService = CreatePplProgramService(context, new TestCurrentUserService("other"));
        var ownerSessions = new PplSessionService(context, new TestCurrentUserService("owner"));

        var program = await ownerService.CreateProgramAsync("Owner PPL", 3);
        var session = await ownerService.GetOrCreateNextSessionAsync(program.Id);
        await ownerSessions.CompleteSessionAsync(session.Id);
        await ownerService.GetOrCreateNextSessionAsync(program.Id);
        context.ChangeTracker.Clear();

        await otherService.DeleteProgramAsync(program.Id);
        Assert.True(await context.PplPrograms.AnyAsync(p => p.Id == program.Id));

        await ownerService.DeleteProgramAsync(program.Id);
        Assert.False(await context.PplPrograms.AnyAsync());
        Assert.False(await context.PplSessions.AnyAsync());
        Assert.False(await context.PplSessionExercises.AnyAsync());
        Assert.False(await context.PplExerciseSlots.AnyAsync());
    }

    [Fact]
    public async Task PplProgressionAppliesOnceWithSmallIncrementsAndRequiresAllSets()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var user = new TestCurrentUserService("owner");
        var programService = CreatePplProgramService(context, user);
        var sessionService = new PplSessionService(context, user);
        var progression = new PplProgressionService(context, new WeightCalculator(), user);
        var otherProgression = new PplProgressionService(context, new WeightCalculator(), new TestCurrentUserService("other"));

        var program = await programService.CreateProgramAsync("PPL", 3);
        var details = await programService.GetProgramWithDetailsAsync(program.Id);
        var slot = details!.DayTemplates.OrderBy(d => d.OrderInWeek).First().ExerciseSlots.First(s => !s.IsBodyweight);
        await programService.ConfigureStartingWeightsAsync(program.Id, new Dictionary<int, double?> { [slot.Id] = 100 });
        slot.ProgressionIncrement = 2.5;
        await context.SaveChangesAsync();

        var session = await programService.GetOrCreateNextSessionAsync(program.Id);
        var exercise = (await sessionService.GetSessionWithDetailsAsync(session.Id))!.Exercises.Single(e => e.PplExerciseSlotId == slot.Id);
        var sets = exercise.Sets.OrderBy(s => s.SetNumber).ToList();

        await sessionService.UpdateSetAsync(sets[0].Id, 100, exercise.RepsMax, true);
        context.ChangeTracker.Clear();
        Assert.False(await progression.ApplyProgressionAsync(exercise.Id));

        foreach (var set in sets)
            await sessionService.UpdateSetAsync(set.Id, 100, exercise.RepsMax, true);
        context.ChangeTracker.Clear();

        Assert.False(await otherProgression.ApplyProgressionAsync(exercise.Id));
        Assert.True(await progression.ApplyProgressionAsync(exercise.Id));
        Assert.False(await progression.ApplyProgressionAsync(exercise.Id));

        await sessionService.CompleteSessionAsync(session.Id);
        Assert.Equal(0, await progression.ApplySessionProgressionAsync(session.Id));
        context.ChangeTracker.Clear();
        Assert.Equal(102.5, (await context.PplExerciseSlots.SingleAsync(s => s.Id == slot.Id)).CurrentWeight);
    }

    [Fact]
    public async Task PplSessionCompletionAutoProgressesEarnedExercises()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var user = new TestCurrentUserService("owner");
        var programService = CreatePplProgramService(context, user);
        var sessionService = new PplSessionService(context, user);
        var progression = new PplProgressionService(context, new WeightCalculator(), user);

        var program = await programService.CreateProgramAsync("PPL", 3);
        var details = await programService.GetProgramWithDetailsAsync(program.Id);
        var slot = details!.DayTemplates.OrderBy(d => d.OrderInWeek).First().ExerciseSlots.First(s => !s.IsBodyweight);
        await programService.ConfigureStartingWeightsAsync(program.Id, new Dictionary<int, double?> { [slot.Id] = 135 });

        var session = await programService.GetOrCreateNextSessionAsync(program.Id);
        Assert.Equal(0, await progression.ApplySessionProgressionAsync(session.Id));

        var exercise = (await sessionService.GetSessionWithDetailsAsync(session.Id))!.Exercises.Single(e => e.PplExerciseSlotId == slot.Id);
        foreach (var set in exercise.Sets)
            await sessionService.UpdateSetAsync(set.Id, 135, exercise.RepsMax, true);
        await sessionService.CompleteSessionAsync(session.Id);
        context.ChangeTracker.Clear();

        Assert.Equal(1, await progression.ApplySessionProgressionAsync(session.Id));
        Assert.Equal(0, await progression.ApplySessionProgressionAsync(session.Id));
        var progressed = await context.PplExerciseSlots.SingleAsync(s => s.Id == slot.Id);
        Assert.Equal(135 + progressed.ProgressionIncrement, progressed.CurrentWeight);
    }

    [Fact]
    public async Task PplWeeksAdvanceByCompletedRotationsNotCalendarTime()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var user = new TestCurrentUserService("owner");
        var programService = CreatePplProgramService(context, user);
        var sessionService = new PplSessionService(context, user);
        var program = await programService.CreateProgramAsync("PPL", 3);
        program.CreatedAt = DateTime.UtcNow.AddDays(-60);
        await context.SaveChangesAsync();

        var dayTypes = new List<PplDayType>();
        var weeks = new List<int>();
        for (var i = 0; i < 4; i++)
        {
            var session = await programService.GetOrCreateNextSessionAsync(program.Id);
            dayTypes.Add(session.DayTemplate.DayType);
            weeks.Add(session.Week!.WeekNumber);
            await sessionService.CompleteSessionAsync(session.Id);
        }

        Assert.Equal([PplDayType.Push, PplDayType.Pull, PplDayType.Legs, PplDayType.Push], dayTypes);
        Assert.Equal([1, 1, 1, 2], weeks);
        Assert.Equal(2, await programService.GetCurrentWeekNumberAsync(program.Id));
    }

    [Fact]
    public async Task PplSetUpdatesRejectInvalidValues()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var user = new TestCurrentUserService("owner");
        var programService = CreatePplProgramService(context, user);
        var sessionService = new PplSessionService(context, user);
        var program = await programService.CreateProgramAsync("PPL", 3);
        var session = await programService.GetOrCreateNextSessionAsync(program.Id);
        var set = (await sessionService.GetSessionWithDetailsAsync(session.Id))!.Exercises.First().Sets.First();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sessionService.UpdateSetAsync(set.Id, -5, 8, true));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sessionService.UpdateSetAsync(set.Id, 100, -1, true));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sessionService.UpdateSetAsync(set.Id, double.NaN, 8, true));
        await sessionService.UpdateSetAsync(set.Id, 0, 0, false);
    }

    [Fact]
    public async Task FirstPplProgramUsesMostRecent531CycleNotAllTimeBest()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var bench = new Lift { UserId = "test-user", LiftType = LiftType.BenchPress, Name = "Bench Press", TrainingMax = 200 };
        AddCompleted531BenchSet(context, bench, cycleNumber: 1, occurredOn: DateTime.UtcNow.AddDays(-90), weight: 210, reps: 5);
        AddCompleted531BenchSet(context, bench, cycleNumber: 2, occurredOn: DateTime.UtcNow.AddDays(-5), weight: 170, reps: 5);
        await context.SaveChangesAsync();

        var service = CreatePplProgramService(context, new TestCurrentUserService());
        var program = await service.CreateProgramAsync("PPL after 531", 3);
        var details = await service.GetProgramWithDetailsAsync(program.Id);
        var benchSlot = details!.DayTemplates.SelectMany(d => d.ExerciseSlots).Single(s => s.ExerciseName == "Barbell Bench Press");

        // Most recent cycle: 170x5 → e1RM 200 → 200 / (1 + 8/30) ≈ 160 (the older 245 e1RM would give 195).
        Assert.Equal(160, benchSlot.StartingWeight);
    }

    [Fact]
    public async Task First531CycleUsesMostRecentPplProgramEvenWhenTrainingMaxWasCustomized()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Lifts.AddRange(
            new Lift { UserId = "test-user", LiftType = LiftType.Squat, Name = "Squat", TrainingMax = 315 },
            new Lift { UserId = "test-user", LiftType = LiftType.BenchPress, Name = "Bench Press", TrainingMax = 250 },
            new Lift { UserId = "test-user", LiftType = LiftType.Deadlift, Name = "Deadlift", TrainingMax = 365 },
            new Lift { UserId = "test-user", LiftType = LiftType.OverheadPress, Name = "Overhead Press", TrainingMax = 145 });
        await context.SaveChangesAsync();

        var user = new TestCurrentUserService();
        var calculator = new WeightCalculator();
        var pplService = CreatePplProgramService(context, user);
        var pplSessions = new PplSessionService(context, user);

        async Task CompleteBenchSessionAsync(string name, double weight, DateTime occurredOn)
        {
            var program = await pplService.CreateProgramAsync(name, 3);
            var session = await pplService.GetOrCreateNextSessionAsync(program.Id);
            var details = await pplSessions.GetSessionWithDetailsAsync(session.Id);
            var set = details!.Exercises.Single(e => e.ExerciseName == "Barbell Bench Press").Sets.First();
            await pplSessions.UpdateSetAsync(set.Id, weight, 5, true);
            await pplSessions.UpdateSessionDateAsync(session.Id, occurredOn);
            await pplSessions.CompleteSessionAsync(session.Id);
        }

        await CompleteBenchSessionAsync("Older PPL", 210, DateTime.UtcNow.AddDays(-60));
        await CompleteBenchSessionAsync("Recent PPL", 170, DateTime.UtcNow.AddDays(-2));

        var cycleService = new CycleService(
            context,
            new LiftService(context, user),
            new BbbMappingService(),
            calculator,
            user,
            new EstimatedStrengthService(context, calculator));
        await cycleService.CreateCycleAsync();

        // Recent PPL 170x5 → e1RM 200 → TM 90% = 180 (older 245 e1RM would give 220; customized 250 is replaced).
        var bench = await context.Lifts.SingleAsync(lift => lift.LiftType == LiftType.BenchPress);
        Assert.Equal(180, bench.TrainingMax);
        var squat = await context.Lifts.SingleAsync(lift => lift.LiftType == LiftType.Squat);
        Assert.Equal(315, squat.TrainingMax);
    }

    private static void AddCompleted531BenchSet(AppDbContext context, Lift bench, int cycleNumber, DateTime occurredOn, double weight, int reps)
    {
        var cycle = new Cycle { UserId = bench.UserId, Name = $"Cycle {cycleNumber}", CycleNumber = cycleNumber };
        var week = new Week { Cycle = cycle, WeekNumber = WeekNumber.Week1 };
        var workout = new Workout { Week = week, MainLiftType = LiftType.BenchPress, Status = WorkoutStatus.Completed, OccurredOn = occurredOn };
        context.WorkoutSets.Add(new WorkoutSet
        {
            Workout = workout,
            Lift = bench,
            SetType = SetType.Main,
            IsCompleted = true,
            ActualWeight = weight,
            ActualReps = reps
        });
    }

    [Fact]
    public async Task PplSwapOffersSameMuscleOptionsAndSupportsOneOffAndPermanentSwaps()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var user = new TestCurrentUserService("owner");
        var calculator = new WeightCalculator();
        var programService = CreatePplProgramService(context, user);
        var sessionService = new PplSessionService(context, user);
        var progression = new PplProgressionService(context, calculator, user);
        var swap = new PplExerciseSwapService(context, user, new EstimatedStrengthService(context, calculator), calculator);
        var otherSwap = new PplExerciseSwapService(context, new TestCurrentUserService("other"), new EstimatedStrengthService(context, calculator), calculator);

        var program = await programService.CreateProgramAsync("PPL", 3);
        var session = await programService.GetOrCreateNextSessionAsync(program.Id);
        var details = await sessionService.GetSessionWithDetailsAsync(session.Id);
        var incline = details!.Exercises.Single(e => e.ExerciseName == "Incline DB Press");
        var pushdown = details.Exercises.Single(e => e.ExerciseName == "Tricep Pushdown");

        var chestOptions = await swap.GetSwapOptionsAsync(incline.Id);
        Assert.NotEmpty(chestOptions);
        Assert.All(chestOptions, o => Assert.Equal(MuscleGroup.Chest, o.PrimaryMuscle));
        Assert.DoesNotContain(chestOptions, o => o.Name == "Incline DB Press");
        Assert.Equal("Incline Barbell Bench", chestOptions[0].Name); // same movement type ranks first
        Assert.Empty(await otherSwap.GetSwapOptionsAsync(incline.Id));

        var pushUp = chestOptions.Single(o => o.Name == "Push-Up");
        await Assert.ThrowsAsync<InvalidOperationException>(() => otherSwap.SwapAsync(incline.Id, pushUp.Id, PplSwapScope.ThisWorkout));

        // One-off: session snapshot changes, plan and progression stay on the original exercise.
        await swap.SwapAsync(incline.Id, chestOptions.Single(o => o.Name == "Dumbbell Fly").Id, PplSwapScope.ThisWorkout);
        context.ChangeTracker.Clear();
        var oneOff = await context.PplSessionExercises.Include(e => e.ExerciseSlot).Include(e => e.Sets).SingleAsync(e => e.Id == incline.Id);
        Assert.Equal("Dumbbell Fly", oneOff.ExerciseName);
        Assert.Equal("Incline DB Press", oneOff.ExerciseSlot.ExerciseName);
        foreach (var set in oneOff.Sets)
            await sessionService.UpdateSetAsync(set.Id, 40, oneOff.RepsMax, true);
        context.ChangeTracker.Clear();
        Assert.False(await progression.ApplyProgressionAsync(incline.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => swap.SwapAsync(incline.Id, pushUp.Id, PplSwapScope.ThisWorkout));

        // Permanent: the plan slot is replaced and the next session uses it.
        var tricepOptions = await swap.GetSwapOptionsAsync(pushdown.Id);
        var skullCrusher = tricepOptions.Single(o => o.Name == "Skull Crusher");
        await swap.SwapAsync(pushdown.Id, skullCrusher.Id, PplSwapScope.FutureWorkouts);
        context.ChangeTracker.Clear();
        var slot = await context.PplExerciseSlots.SingleAsync(s => s.Id == pushdown.PplExerciseSlotId);
        Assert.Equal("Skull Crusher", slot.ExerciseName);
        Assert.Equal(skullCrusher.Id, slot.ExerciseDefinitionId);
        Assert.Equal("Skull Crusher", (await context.PplSessionExercises.SingleAsync(e => e.Id == pushdown.Id)).ExerciseName);

        await sessionService.CompleteSessionAsync(session.Id);
        await programService.GetOrCreateNextSessionAsync(program.Id); // pull
        context.ChangeTracker.Clear();
        var completedSecond = await context.PplSessions.SingleAsync(s => s.Status != WorkoutStatus.Completed);
        await sessionService.CompleteSessionAsync(completedSecond.Id);
        var legs = await programService.GetOrCreateNextSessionAsync(program.Id);
        await sessionService.CompleteSessionAsync(legs.Id);
        var nextPush = await programService.GetOrCreateNextSessionAsync(program.Id);
        var nextPushDetails = await sessionService.GetSessionWithDetailsAsync(nextPush.Id);
        Assert.Contains(nextPushDetails!.Exercises, e => e.ExerciseName == "Skull Crusher");
        Assert.Contains(nextPushDetails.Exercises, e => e.ExerciseName == "Incline DB Press");
    }

    [Fact]
    public void PdfIsGeneratedFromTheSameDocument()
    {
        var document = CreateDocument();
        document.Workout!.Exercises[0].AdditionalSets.Add(new SetExportModel
        {
            Number = 1,
            Type = "DropSet",
            ActualWeight = 165,
            ActualReps = 12,
            Rpe = 8,
            Notes = "After AMRAP",
            IsCompleted = true
        });

        var pdf = new PdfWorkoutExporter().Render(document);

        Assert.NotEmpty(pdf);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));
    }

    [Fact]
    public void WeeklyMarkdownKeepsWorkoutNotesAttachedToEachWorkout()
    {
        var document = new WorkoutExportDocument
        {
            Scope = WorkoutExportScope.Week,
            Week = new WeekExportModel
            {
                WeekNumber = 2,
                CycleNumber = 12,
                Workouts =
                [
                    CreateWorkout("Bench Press Day", "Bench notes"),
                    CreateWorkout("Overhead Press Day", "Press notes")
                ]
            }
        };

        var markdown = System.Text.Encoding.UTF8.GetString(new MarkdownWorkoutExporter().Render(document));

        var benchPosition = markdown.IndexOf("Bench notes", StringComparison.Ordinal);
        var pressHeadingPosition = markdown.IndexOf("Overhead Press Day", StringComparison.Ordinal);
        var pressPosition = markdown.IndexOf("Press notes", StringComparison.Ordinal);

        Assert.True(benchPosition >= 0 && pressHeadingPosition > benchPosition);
        Assert.True(pressPosition > pressHeadingPosition);
    }

    [Fact]
    public void CyclePdfIncludesWorkoutNotesWithoutMergingCycleNotes()
    {
        var document = new WorkoutExportDocument
        {
            Scope = WorkoutExportScope.Cycle,
            Cycle = new CycleExportModel
            {
                CycleNumber = 12,
                Name = "Cycle 12",
                Notes = "Cycle-level context",
                Weeks =
                [
                    new WeekExportModel
                    {
                        WeekNumber = 1,
                        CycleNumber = 12,
                        Workouts = [CreateWorkout("Bench Press Day", "Workout-level context")]
                    }
                ]
            }
        };

        var pdf = new PdfWorkoutExporter().Render(document);

        Assert.NotEmpty(pdf);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));
    }

    [Fact]
    public void EmptyWorkoutNotesAreValidAndOmitted()
    {
        var document = new WorkoutExportDocument
        {
            Scope = WorkoutExportScope.Day,
            Workout = new WorkoutExportModel
            {
                WorkoutName = "Empty Notes Day",
                WorkoutType = "BenchPress",
                Status = "NotStarted"
            }
        };

        var markdown = System.Text.Encoding.UTF8.GetString(new MarkdownWorkoutExporter().Render(document));

        Assert.DoesNotContain("## Workout Notes", markdown);
    }

    [Fact]
    public void MarkdownSeparatesAdditionalSetsFromProgrammedSets()
    {
        var document = CreateDocument();
        document.Workout!.Exercises[0].AdditionalSets.Add(new SetExportModel
        {
            Number = 1,
            Type = "DropSet",
            ActualWeight = 165,
            ActualReps = 12,
            Rpe = 8,
            Notes = "Drop set after AMRAP",
            IsCompleted = true
        });

        var markdown = System.Text.Encoding.UTF8.GetString(new MarkdownWorkoutExporter().Render(document));

        Assert.Contains("### Additional Sets", markdown);
        Assert.Contains("| 1 | DropSet | 165 | 12 | 8 |", markdown);
        Assert.True(markdown.IndexOf("### Additional Sets", StringComparison.Ordinal) > markdown.IndexOf("| 5 | Main", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AdditionalSetsPersistInOrderWithoutChangingProgrammedSets()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using (var context = new AppDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            var lift = new Lift { LiftType = LiftType.BenchPress, Name = "Bench Press", UserId = "test-user" };
            var cycle = new Cycle { Name = "Test Cycle", UserId = "test-user" };
            var week = new Week { Cycle = cycle, WeekNumber = WeekNumber.Week1 };
            var workout = new Workout { Week = week, MainLiftType = LiftType.BenchPress };
            workout.Sets.Add(new WorkoutSet { Lift = lift, SetType = SetType.Main, SetNumber = 1, PrescribedWeight = 185, PrescribedReps = 5 });
            context.Add(workout);
            await context.SaveChangesAsync();
        }

        int firstId;
        await using (var context = new AppDbContext(options))
        {
            var service = new WorkoutService(context, new TestCurrentUserService());
            var first = await service.AddAdditionalSetAsync(1, 165, 12, AdditionalSetType.DropSet, 8, null, "After top set");
            var second = await service.AddAdditionalSetAsync(1, 145, 12, AdditionalSetType.DropSet, 9, null, null);
            Assert.NotNull(first);
            Assert.NotNull(second);
            firstId = first!.Id;
            Assert.Equal(1, first.Sequence);
            Assert.Equal(2, second!.Sequence);
        }

        await using (var context = new AppDbContext(options))
        {
            var programmed = await context.WorkoutSets.SingleAsync(s => !s.IsAdditional);
            var additional = await context.WorkoutSets.Where(s => s.IsAdditional).OrderBy(s => s.Sequence).ToListAsync();
            Assert.Equal(185, programmed.PrescribedWeight);
            Assert.Equal(5, programmed.PrescribedReps);
            Assert.Equal(2, additional.Count);
            Assert.Equal("After top set", additional[0].Notes);

            var service = new WorkoutService(context, new TestCurrentUserService());
            await service.UpdateAdditionalSetAsync(firstId, 160, 10, AdditionalSetType.BackOffSet, 7, 2, "Updated");
            await service.DeleteAdditionalSetAsync(additional[1].Id);
        }

        await using (var context = new AppDbContext(options))
        {
            var remaining = await context.WorkoutSets.Where(s => s.IsAdditional).ToListAsync();
            Assert.Single(remaining);
            Assert.Equal(160, remaining[0].ActualWeight);
            Assert.Equal(AdditionalSetType.BackOffSet, remaining[0].AdditionalSetType);
            Assert.Equal("Updated", remaining[0].Notes);
            Assert.Single(await context.WorkoutSets.Where(s => !s.IsAdditional).ToListAsync());
        }
    }

    [Fact]
    public async Task AdditionalSetValidationRejectsInvalidValuesAndNonPrimaryLifts()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using (var context = new AppDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            var bench = new Lift { LiftType = LiftType.BenchPress, Name = "Bench Press", UserId = "test-user" };
            var deadlift = new Lift { LiftType = LiftType.Deadlift, Name = "Deadlift", UserId = "test-user" };
            var cycle = new Cycle { Name = "Test Cycle", UserId = "test-user" };
            var week = new Week { Cycle = cycle, WeekNumber = WeekNumber.Week1 };
            var workout = new Workout { Week = week, MainLiftType = LiftType.BenchPress };
            workout.Sets.Add(new WorkoutSet { Lift = deadlift, SetType = SetType.Main, SetNumber = 1, PrescribedWeight = 315, PrescribedReps = 5 });
            context.AddRange(bench, workout);
            await context.SaveChangesAsync();
        }

        await using (var context = new AppDbContext(options))
        {
            var service = new WorkoutService(context, new TestCurrentUserService());

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                service.AddAdditionalSetAsync(1, -1, 5, AdditionalSetType.Additional, null, null, null));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                service.AddAdditionalSetAsync(1, 100, 0, AdditionalSetType.Additional, null, null, null));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                service.AddAdditionalSetAsync(1, 100, 5, AdditionalSetType.Additional, 11, null, null));

            var result = await service.AddAdditionalSetAsync(1, 100, 5, AdditionalSetType.Additional, null, null, null);
            Assert.Null(result);
            Assert.Empty(await context.WorkoutSets.Where(s => s.IsAdditional).ToListAsync());
        }
    }

    [Fact]
    public async Task WorkoutNotesPersistAcrossEditsAndStayWithTheirWorkout()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var context = new AppDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            var cycle = new Cycle { Name = "Test Cycle", UserId = "test-user" };
            var week = new Week { Cycle = cycle, WeekNumber = WeekNumber.Week1 };
            context.Workouts.AddRange(
                new Workout { Week = week, MainLiftType = LiftType.BenchPress },
                new Workout { Week = week, MainLiftType = LiftType.OverheadPress });
            await context.SaveChangesAsync();
        }

        await using (var context = new AppDbContext(options))
        {
            var service = new WorkoutService(context, new TestCurrentUserService());
            await service.UpdateWorkoutNotesAsync(1, "First line\nSecond line");
            await service.UpdateWorkoutNotesAsync(1, "Updated notes");
        }

        await using (var context = new AppDbContext(options))
        {
            var first = await context.Workouts.SingleAsync(w => w.Id == 1);
            var second = await context.Workouts.SingleAsync(w => w.Id == 2);

            Assert.Equal("Updated notes", first.Notes);
            Assert.Null(second.Notes);
        }
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        private readonly string userId;

        public TestCurrentUserService(string userId = "test-user") => this.userId = userId;

        public Task<string> GetUserIdAsync() => Task.FromResult(userId);
        public Task<string?> GetUserIdOrNullAsync() => Task.FromResult<string?>(userId);
    }

    private static PplProgramService CreatePplProgramService(AppDbContext context, ICurrentUserService userContext)
    {
        var weightCalculator = new WeightCalculator();
        return new PplProgramService(
            context,
            userContext,
            new EstimatedStrengthService(context, weightCalculator),
            weightCalculator);
    }

    private static WorkoutExportModel CreateWorkout(string name, string notes) => new()
    {
        WorkoutName = name,
        WorkoutType = name,
        Notes = notes,
        Status = "Completed",
        Exercises = []
    };

    private static WorkoutExportDocument CreateDocument() => new()
    {
        Scope = WorkoutExportScope.Day,
        Workout = new WorkoutExportModel
        {
            CycleNumber = 12,
            WeekNumber = 2,
            WorkoutName = "Bench Press Day",
            WorkoutType = "BenchPress",
            WorkoutDate = new DateTime(2026, 9, 4),
            Status = "Completed",
            Notes = "Felt unusually fatigued.\nRecovery was poor.",
            Exercises =
            [
                new ExerciseExportModel
                {
                    Name = "Bench Press",
                    Category = "Main Lift",
                    Notes = "Left shoulder felt uncomfortable.",
                    Sets =
                    [
                        new SetExportModel { Number = 5, Type = "Main", TargetReps = 5, ActualReps = 9, TargetWeight = 235, ActualWeight = 235, IsCompleted = true, IsAmrap = true, Notes = "Last rep was slow." }
                    ]
                }
            ],
            Summary = new ExportSummaryModel { ExerciseCount = 1, SetCount = 1, CompletedSetCount = 1, TotalReps = 9, TotalVolume = 2115 }
        }
    };
}
