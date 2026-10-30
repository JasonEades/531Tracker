using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using FiveThreeOneTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace FiveThreeOneTracker.Tests;

public sealed class ExerciseLibraryTests
{
    [Fact]
    public async Task LibrarySearchFiltersAndCustomExercisesArePrivateToTheirOwner()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Users.AddRange(
            new ApplicationUser { Id = "owner", UserName = "owner", NormalizedUserName = "OWNER" },
            new ApplicationUser { Id = "other", UserName = "other", NormalizedUserName = "OTHER" });
        await context.SaveChangesAsync();

        var ownerService = new ExerciseLibraryService(context, new TestCurrentUserService("owner"));
        var otherService = new ExerciseLibraryService(context, new TestCurrentUserService("other"));
        var custom = await ownerService.CreateCustomExerciseAsync(
            "Cable Y Raise", MuscleGroup.Shoulders, "Traps", "Cable", "Shoulder isolation", "Raise the handles in a controlled Y path.");

        var ownerExercises = await ownerService.GetExercisesAsync(search: "cable", muscle: MuscleGroup.Shoulders, equipment: "Cable");
        var otherExercises = await otherService.GetExercisesAsync();

        Assert.Contains(ownerExercises, exercise => exercise.Id == custom.Id);
        Assert.DoesNotContain(ownerExercises, exercise => exercise.Name == "Cable Fly");
        Assert.DoesNotContain(otherExercises, exercise => exercise.Id == custom.Id);
        Assert.Null(await otherService.GetExerciseAsync(custom.Id));
        Assert.NotNull(await ownerService.GetExerciseAsync(custom.Id));
        Assert.Contains(otherExercises, exercise => exercise.Name == "Cable Fly");
    }

    [Fact]
    public async Task PplWorkoutCanSelectCatalogExerciseAndGeneratedSessionKeepsNameSnapshot()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Users.Add(new ApplicationUser { Id = "owner", UserName = "owner", NormalizedUserName = "OWNER" });
        await context.SaveChangesAsync();

        var user = new TestCurrentUserService("owner");
        var calculator = new WeightCalculator();
        var programService = new PplProgramService(
            context, user, new EstimatedStrengthService(context, calculator), calculator);
        var libraryService = new ExerciseLibraryService(context, user);
        var program = await programService.CreateProgramAsync("Library PPL", 3);
        var defaultDays = await programService.GetWeekWorkoutTemplatesAsync(program.Id, 1);
        var custom = await libraryService.CreateCustomExerciseAsync(
            "Private Pec Fly", MuscleGroup.Chest, "Shoulders", "Cable", "Horizontal adduction", "Bring the handles together with control.");
        var drafts = defaultDays.Select((day, dayIndex) => new PplWorkoutDayDraft(
            day.Id,
            day.Name,
            day.ExerciseSlots.OrderBy(slot => slot.OrderInDay).Select((slot, slotIndex) =>
            {
                var useCustom = dayIndex == 0 && slotIndex == 0;
                return new PplWorkoutExerciseDraft(
                    slot.Id,
                    useCustom ? custom.Name : slot.ExerciseName,
                    useCustom ? custom.PrimaryMuscle : slot.MuscleGroup,
                    slot.TargetSets,
                    slot.RepsMin,
                    slot.RepsMax,
                    slot.IsBodyweight,
                    slot.ProgressionIncrement,
                    slot.StartingWeight,
                    slot.CurrentWeight,
                    useCustom ? custom.Id : slot.ExerciseDefinitionId);
            }).ToList())).ToList();

        await programService.ConfigureWeekWorkoutsAsync(program.Id, 1, drafts);
        var configured = await programService.GetWeekWorkoutTemplatesAsync(program.Id, 1);
        var selectedSlot = configured[0].ExerciseSlots.OrderBy(slot => slot.OrderInDay).First();
        Assert.Equal(custom.Id, selectedSlot.ExerciseDefinitionId);
        Assert.Equal(custom.Name, selectedSlot.ExerciseName);

        var session = await programService.GetOrCreateNextSessionAsync(program.Id);
        var sessionExercise = await context.PplSessionExercises
            .SingleAsync(exercise => exercise.PplSessionId == session.Id && exercise.PplExerciseSlotId == selectedSlot.Id);
        selectedSlot.ExerciseName = "Renamed later";
        await context.SaveChangesAsync();

        Assert.Equal(custom.Name, sessionExercise.ExerciseName);
    }

    [Fact]
    public async Task MigrationsCreateCuratedExerciseCatalog()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        await using var context = new AppDbContext(options);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20261011152626_AddPplWeeklyWorkoutTemplates");

        await InsertLegacyPplProgramAsync(context);
        var day = new PplDayTemplate
        {
            PplProgramId = LegacyProgramId,
            DayType = PplDayType.Push,
            OrderInWeek = 1,
            Name = "Legacy Push"
        };
        context.PplDayTemplates.Add(day);
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "PplExerciseSlots"
                ("PplDayTemplateId", "OrderInDay", "ExerciseName", "MuscleGroup", "TargetSets",
                 "RepsMin", "RepsMax", "UsePercentageOfTm", "TmPercentage", "LiftId",
                 "StartingWeight", "CurrentWeight", "ProgressionIncrement", "IsBodyweight")
            VALUES
                ({day.Id}, 1, 'Barbell Bench Press', 'Chest', 3, 8, 12, 0, 0, NULL,
                 NULL, NULL, 5, 0),
                ({day.Id}, 2, 'DB Shoulder Press', 'Shoulders', 3, 8, 12, 0, 0, NULL,
                 NULL, NULL, 5, 0)
            """);

        await migrator.MigrateAsync();

        Assert.Equal(77, await context.ExerciseDefinitions.CountAsync(exercise => exercise.UserId == null));
        var legacySlots = await context.PplExerciseSlots.OrderBy(slot => slot.OrderInDay).ToListAsync();
        Assert.Equal("Barbell Bench Press", legacySlots[0].ExerciseName);
        Assert.Equal(-1, legacySlots[0].ExerciseDefinitionId);
        Assert.Equal("DB Shoulder Press", legacySlots[1].ExerciseName);
        Assert.Equal(-37, legacySlots[1].ExerciseDefinitionId);
        var migrations = await context.Database.GetAppliedMigrationsAsync();
        Assert.Contains(migrations, migration => migration.EndsWith("_AddExerciseLibrary", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MergeMigrationRepointsDuplicateSlotsAndLinksAccessoriesWithoutTouchingHistory()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        await using var context = new AppDbContext(options);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20261027211549_AddPplExerciseBars");

        context.Users.AddRange(
            new ApplicationUser { Id = "owner", UserName = "owner" },
            new ApplicationUser { Id = "other", UserName = "other" });
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO "ExerciseDefinitions" ("Id", "Name", "PrimaryMuscle", "SecondaryMuscles", "Equipment", "MovementType", "Instructions", "UserId")
            VALUES (500, 'Sled Push', 'Quads', '', '', '', 'Push it.', 'owner');
            INSERT INTO "Accessories" ("Id", "Name", "Category", "IsActive", "UserId")
            VALUES (100, 'Face Pull', 'Strength', 1, 'owner'),
                   (101, 'sled push', 'Strength', 1, 'owner'),
                   (102, 'Sled Push', 'Strength', 1, 'other'),
                   (103, 'Lat Pulldown', 'Strength', 1, 'owner'),
                   (104, 'Zercher Carry Thing', 'Strength', 1, 'owner'),
                   (105, 'Rowing', 'Cardio', 1, 'owner');
            """);
        await InsertLegacyPplProgramAsync(context);
        var day = new PplDayTemplate { PplProgramId = LegacyProgramId, DayType = PplDayType.Pull, OrderInWeek = 1, Name = "Pull" };
        day.ExerciseSlots.Add(new PplExerciseSlot { OrderInDay = 1, ExerciseName = "Face Pull", ExerciseDefinitionId = -33 });
        day.ExerciseSlots.Add(new PplExerciseSlot { OrderInDay = 2, ExerciseName = "Seated Cable Row", ExerciseDefinitionId = -55 });
        context.PplDayTemplates.Add(day);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await migrator.MigrateAsync();

        var slots = await context.PplExerciseSlots.AsNoTracking().OrderBy(slot => slot.OrderInDay).ToListAsync();
        Assert.Equal(("Face Pull", (int?)-13), (slots[0].ExerciseName, slots[0].ExerciseDefinitionId));
        Assert.Equal(("Seated Cable Row", (int?)-10), (slots[1].ExerciseName, slots[1].ExerciseDefinitionId));

        var links = await context.Accessories.AsNoTracking()
            .Where(accessory => accessory.Id >= 100 || accessory.Id <= 4)
            .ToDictionaryAsync(accessory => accessory.Id, accessory => accessory.ExerciseDefinitionId);
        Assert.Equal(-13, links[4]);
        Assert.Equal(-13, links[100]);
        Assert.Equal(500, links[101]);
        Assert.Null(links[102]);
        Assert.NotNull(links[103]);
        Assert.Null(links[104]);
        Assert.Null(links[105]);
        Assert.Equal("Face Pull", (await context.Accessories.AsNoTracking().SingleAsync(a => a.Id == 100)).Name);
    }
    private const int LegacyProgramId = 1;

    // Raw SQL so the insert does not depend on PplProgram columns added by later migrations.
    private static Task InsertLegacyPplProgramAsync(AppDbContext context) =>
        context.Database.ExecuteSqlRawAsync("""
            INSERT INTO "PplPrograms" ("Id", "Name", "CreatedAt", "DaysPerWeek", "IsActive")
            VALUES (1, 'Legacy PPL', '2026-01-01 00:00:00', 3, 1);
            """);

    private sealed class TestCurrentUserService(string userId) : ICurrentUserService
    {
        public Task<string> GetUserIdAsync() => Task.FromResult(userId);
        public Task<string?> GetUserIdOrNullAsync() => Task.FromResult<string?>(userId);
    }
}
