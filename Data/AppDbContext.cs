using FiveThreeOneTracker.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options), IDataProtectionKeyContext
{
    // Data Protection keys — persisted to DB so keys survive container restarts
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    // ── 5/3/1 ────────────────────────────────────────────────────────────────
    public DbSet<Lift> Lifts => Set<Lift>();
    public DbSet<Cycle> Cycles => Set<Cycle>();
    public DbSet<Week> Weeks => Set<Week>();
    public DbSet<Workout> Workouts => Set<Workout>();
    public DbSet<WorkoutSet> WorkoutSets => Set<WorkoutSet>();
    public DbSet<Accessory> Accessories => Set<Accessory>();
    public DbSet<WorkoutAccessory> WorkoutAccessories => Set<WorkoutAccessory>();
    public DbSet<AdditionalSession> AdditionalSessions => Set<AdditionalSession>();
    public DbSet<AdditionalStrengthExercise> AdditionalStrengthExercises => Set<AdditionalStrengthExercise>();
    public DbSet<AdditionalStrengthSet> AdditionalStrengthSets => Set<AdditionalStrengthSet>();
    public DbSet<CardioEntry> CardioEntries => Set<CardioEntry>();
    public DbSet<AccessoryHistory> AccessoryHistory => Set<AccessoryHistory>();
    public DbSet<UserEquipment> UserEquipment => Set<UserEquipment>();
    public DbSet<PlateInventory> PlateInventory => Set<PlateInventory>();
    public DbSet<Bar> Bars => Set<Bar>();
    public DbSet<UserProtocol> UserProtocols => Set<UserProtocol>();
    public DbSet<GoogleHealthConnection> GoogleHealthConnections => Set<GoogleHealthConnection>();
    public DbSet<DailyStepRecord> DailyStepRecords => Set<DailyStepRecord>();
    public DbSet<SiteContent> SiteContents => Set<SiteContent>();
    public DbSet<SiteContentImage> SiteContentImages => Set<SiteContentImage>();

    // ── Body metrics ─────────────────────────────────────────────────────────
    public DbSet<BodyMetricEntry> BodyMetricEntries => Set<BodyMetricEntry>();
    public DbSet<ProgressPhoto> ProgressPhotos => Set<ProgressPhoto>();

    // ── PPL ──────────────────────────────────────────────────────────────────
    public DbSet<PplProgram> PplPrograms => Set<PplProgram>();
    public DbSet<PplDayTemplate> PplDayTemplates => Set<PplDayTemplate>();
    public DbSet<PplExerciseSlot> PplExerciseSlots => Set<PplExerciseSlot>();
    public DbSet<ExerciseDefinition> ExerciseDefinitions => Set<ExerciseDefinition>();
    public DbSet<PplSession> PplSessions => Set<PplSession>();
    public DbSet<PplSessionExercise> PplSessionExercises => Set<PplSessionExercise>();
    public DbSet<PplSessionSet> PplSessionSets => Set<PplSessionSet>();
    public DbSet<PplWeek> PplWeeks => Set<PplWeek>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Lift>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.LiftType }).IsUnique();
            entity.Property(e => e.LiftType).HasConversion<string>();
        });

        modelBuilder.Entity<Cycle>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.StartDate });
            entity.Property(e => e.BbbMode).HasConversion<string>();
            entity.HasMany(c => c.Weeks)
                  .WithOne(w => w.Cycle)
                  .HasForeignKey(w => w.CycleId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(c => c.AdditionalSessions)
                  .WithOne(s => s.Cycle)
                  .HasForeignKey(s => s.CycleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Week>(entity =>
        {
            entity.Property(e => e.WeekNumber).HasConversion<int>();
            entity.HasMany(w => w.Workouts)
                  .WithOne(wo => wo.Week)
                  .HasForeignKey(wo => wo.WeekId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(w => w.AdditionalSessions)
                  .WithOne(s => s.Week)
                  .HasForeignKey(s => s.WeekId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Workout>(entity =>
        {
            entity.Property(e => e.MainLiftType).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasIndex(e => new { e.WeekId, e.OccurredOn });
            entity.HasMany(w => w.Sets)
                  .WithOne(s => s.Workout)
                  .HasForeignKey(s => s.WorkoutId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(w => w.WorkoutAccessories)
                  .WithOne(wa => wa.Workout)
                  .HasForeignKey(wa => wa.WorkoutId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(w => w.Bar)
                  .WithMany()
                  .HasForeignKey(w => w.BarId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WorkoutSet>(entity =>
        {
            entity.Property(e => e.SetType).HasConversion<string>();
            entity.Property(e => e.AdditionalSetType).HasConversion<string>();
            entity.HasIndex(e => new { e.WorkoutId, e.LiftId, e.IsAdditional, e.Sequence });
        });

        modelBuilder.Entity<WorkoutAccessory>(entity =>
        {
            entity.HasOne(wa => wa.Accessory)
                  .WithMany(a => a.WorkoutAccessories)
                  .HasForeignKey(wa => wa.AccessoryId);
        });

        modelBuilder.Entity<Accessory>(entity =>
        {
            entity.Property(e => e.Category).HasConversion<string>();
            entity.HasOne(e => e.ExerciseDefinition)
                  .WithMany()
                  .HasForeignKey(e => e.ExerciseDefinitionId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AccessoryHistory>(entity =>
        {
            entity.HasIndex(ah => new { ah.UserId, ah.AccessoryId, ah.RecordedAt });
            entity.HasOne(ah => ah.Accessory)
                  .WithMany(a => a.History)
                  .HasForeignKey(ah => ah.AccessoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PplWeek>(entity =>
        {
            entity.HasIndex(e => new { e.PplProgramId, e.WeekNumber }).IsUnique();
            entity.HasMany(w => w.Sessions)
                  .WithOne(s => s.Week)
                  .HasForeignKey(s => s.PplWeekId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(w => w.AdditionalSessions)
                  .WithOne(s => s.PplWeek)
                  .HasForeignKey(s => s.PplWeekId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserEquipment>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasMany(e => e.Plates)
                  .WithOne(p => p.UserEquipment)
                  .HasForeignKey(p => p.UserEquipmentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Bar>(entity =>
        {
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<UserProtocol>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        // ── PPL relationships ─────────────────────────────────────────────────

        modelBuilder.Entity<PplProgram>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasMany(p => p.DayTemplates)
                  .WithOne(d => d.Program)
                  .HasForeignKey(d => d.PplProgramId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(p => p.Sessions)
                  .WithOne(s => s.Program)
                  .HasForeignKey(s => s.PplProgramId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(p => p.Weeks)
                  .WithOne(w => w.Program)
                  .HasForeignKey(w => w.PplProgramId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PplDayTemplate>(entity =>
        {
            entity.Property(e => e.DayType).HasConversion<string>();
            entity.Property(e => e.Variant).HasConversion<string>();
            entity.HasIndex(e => new { e.PplProgramId, e.WeekNumber, e.OrderInWeek });
            entity.HasMany(d => d.ExerciseSlots)
                  .WithOne(s => s.DayTemplate)
                  .HasForeignKey(s => s.PplDayTemplateId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(d => d.Sessions)
                  .WithOne(s => s.DayTemplate)
                  .HasForeignKey(s => s.PplDayTemplateId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PplExerciseSlot>(entity =>
        {
            entity.Property(e => e.MuscleGroup).HasConversion<string>();
            entity.Property(e => e.StartingWeight);
            entity.Property(e => e.CurrentWeight);
            entity.HasOne(e => e.ExerciseDefinition)
                  .WithMany()
                  .HasForeignKey(e => e.ExerciseDefinitionId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Bar)
                  .WithMany()
                  .HasForeignKey(e => e.BarId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Lift)
                  .WithMany()
                  .HasForeignKey(e => e.LiftId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ExerciseDefinition>(entity =>
        {
            entity.Property(e => e.PrimaryMuscle).HasConversion<string>();
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PplSession>(entity =>
        {
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasIndex(e => new { e.PplProgramId, e.OccurredOn });
            entity.HasMany(s => s.Exercises)
                  .WithOne(e => e.Session)
                  .HasForeignKey(e => e.PplSessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AdditionalSession>(entity =>
        {
            entity.Property(e => e.SessionType).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasIndex(e => new { e.WeekId, e.OccurredOn });
            entity.HasIndex(e => new { e.PplWeekId, e.OccurredOn });
            entity.HasMany(s => s.StrengthExercises)
                  .WithOne(e => e.Session)
                  .HasForeignKey(e => e.AdditionalSessionId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(s => s.CardioEntries)
                  .WithOne(e => e.Session)
                  .HasForeignKey(e => e.AdditionalSessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GoogleHealthConnection>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Status).HasConversion<string>();
        });

        modelBuilder.Entity<DailyStepRecord>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Provider, e.Metric, e.LocalDate }).IsUnique();
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.LocalDate).HasColumnType("date");
        });

        modelBuilder.Entity<BodyMetricEntry>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.RecordedOn }).IsUnique();
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.RecordedOn).HasColumnType("date");
        });

        modelBuilder.Entity<ProgressPhoto>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.TakenOn });
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.TakenOn).HasColumnType("date");
        });

        modelBuilder.Entity<SiteContent>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => new { e.Type, e.Status, e.PublishedAtUtc });
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.FeaturedImageUrl).HasMaxLength(1000);
            entity.Property(e => e.FeaturedImageCaption).HasMaxLength(240);
            entity.HasMany(e => e.Images)
                .WithOne(e => e.SiteContent)
                .HasForeignKey(e => e.SiteContentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SiteContentImage>(entity =>
        {
            entity.HasIndex(e => new { e.SiteContentId, e.SortOrder });
            entity.Property(e => e.Url).HasMaxLength(1000);
            entity.Property(e => e.Caption).HasMaxLength(240);
        });

        modelBuilder.Entity<AdditionalStrengthExercise>(entity =>
        {
            entity.HasMany(e => e.Sets)
                  .WithOne(s => s.Exercise)
                  .HasForeignKey(s => s.AdditionalStrengthExerciseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CardioEntry>(entity =>
        {
            entity.Property(e => e.Unit).HasConversion<string>();
            entity.HasIndex(e => e.DailyStepRecordId).IsUnique();
            entity.HasOne(e => e.DailyStepRecord)
                  .WithOne(r => r.CardioEntry)
                  .HasForeignKey<CardioEntry>(e => e.DailyStepRecordId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Accessory)
                  .WithMany()
                  .HasForeignKey(e => e.AccessoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PplSessionExercise>(entity =>
        {
            entity.HasOne(e => e.ExerciseSlot)
                  .WithMany(s => s.SessionExercises)
                  .HasForeignKey(e => e.PplExerciseSlotId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Bar)
                  .WithMany()
                  .HasForeignKey(e => e.BarId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(e => e.Sets)
                  .WithOne(s => s.SessionExercise)
                  .HasForeignKey(s => s.PplSessionExerciseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Accessory>().HasData(
            new Accessory { Id = 1, ExerciseDefinitionId = -9, Name = "Barbell Row", Description = "Bent-over barbell row", Category = AccessoryCategory.Strength },
            new Accessory { Id = 2, ExerciseDefinitionId = -32, Name = "Dumbbell Row", Description = "Single-arm dumbbell row" },
            new Accessory { Id = 3, ExerciseDefinitionId = -8, Name = "Lat Pulldown", Description = "Cable lat pulldown" },
            new Accessory { Id = 4, ExerciseDefinitionId = -13, Name = "Face Pulls", Description = "Cable face pull" },
            new Accessory { Id = 5, ExerciseDefinitionId = -27, Name = "Dips", Description = "Parallel bar dips" },
            new Accessory { Id = 6, ExerciseDefinitionId = -34, Name = "Chin-Ups", Description = "Chin-ups / pull-ups" },
            new Accessory { Id = 7, ExerciseDefinitionId = -19, Name = "Leg Curl", Description = "Lying or seated leg curl" },
            new Accessory { Id = 8, ExerciseDefinitionId = -18, Name = "Leg Press", Description = "Machine leg press" },
            new Accessory { Id = 9, ExerciseDefinitionId = -35, Name = "Ab Wheel", Description = "Ab wheel rollout" },
            new Accessory { Id = 10, ExerciseDefinitionId = -36, Name = "Hanging Leg Raise", Description = "Hanging leg raise" },
            new Accessory { Id = 11, ExerciseDefinitionId = -11, Name = "Dumbbell Curl", Description = "Standing dumbbell curl" },
            new Accessory { Id = 12, ExerciseDefinitionId = -5, Name = "Tricep Pushdown", Description = "Cable tricep pushdown" },
            new Accessory { Id = 13, ExerciseDefinitionId = -4, Name = "Lateral Raise", Description = "Dumbbell lateral raise" },
            new Accessory { Id = 14, ExerciseDefinitionId = -17, Name = "Romanian Deadlift", Description = "Romanian deadlift" },
            new Accessory { Id = 15, ExerciseDefinitionId = -22, Name = "Bulgarian Split Squat", Description = "Rear foot elevated split squat", Category = AccessoryCategory.Strength },
            new Accessory { Id = 16, Name = "Rowing", Category = AccessoryCategory.Cardio },
            new Accessory { Id = 17, Name = "Biking", Category = AccessoryCategory.Cardio },
            new Accessory { Id = 18, Name = "Walking", Category = AccessoryCategory.Cardio },
            new Accessory { Id = 19, Name = "Running", Category = AccessoryCategory.Cardio },
            new Accessory { Id = 20, Name = "Treadmill", Category = AccessoryCategory.Cardio },
            new Accessory { Id = 21, Name = "Elliptical", Category = AccessoryCategory.Cardio },
            new Accessory { Id = 22, Name = "Stair Climber", Category = AccessoryCategory.Cardio },
            new Accessory { Id = 23, Name = "Swimming", Category = AccessoryCategory.Cardio },
            new Accessory { Id = 24, Name = "Rucking", Category = AccessoryCategory.Cardio },
            new Accessory { Id = 25, Name = "Other", Category = AccessoryCategory.Cardio },
            new Accessory { Id = 26, Name = "Steps", Category = AccessoryCategory.Cardio }
        );

        modelBuilder.Entity<ExerciseDefinition>().HasData(
            new ExerciseDefinition { Id = -1, Name = "Barbell Bench Press", PrimaryMuscle = MuscleGroup.Chest, SecondaryMuscles = "Triceps, shoulders", Equipment = "Barbell", MovementType = "Horizontal push", Instructions = "Lie on the bench with feet planted. Lower the bar under control to the mid-chest, then press it upward until your arms are extended." },
            new ExerciseDefinition { Id = -2, Name = "Incline DB Press", PrimaryMuscle = MuscleGroup.Chest, SecondaryMuscles = "Triceps, shoulders", Equipment = "Dumbbell, incline bench", MovementType = "Incline push", Instructions = "Set the bench to a low incline. Lower the dumbbells beside the upper chest, then press them up without letting them collide." },
            new ExerciseDefinition { Id = -3, Name = "Overhead Press", PrimaryMuscle = MuscleGroup.Shoulders, SecondaryMuscles = "Triceps, upper chest", Equipment = "Barbell", MovementType = "Vertical push", Instructions = "Start with the bar at shoulder height. Brace your trunk and press overhead, moving your head clear of the bar." },
            new ExerciseDefinition { Id = -4, Name = "Lateral Raise", PrimaryMuscle = MuscleGroup.Shoulders, SecondaryMuscles = "Traps", Equipment = "Dumbbell", MovementType = "Shoulder isolation", Instructions = "With a slight bend in your elbows, raise the weights out to your sides to about shoulder height, then lower them under control." },
            new ExerciseDefinition { Id = -5, Name = "Tricep Pushdown", PrimaryMuscle = MuscleGroup.Triceps, SecondaryMuscles = "Forearms", Equipment = "Cable", MovementType = "Elbow extension", Instructions = "Keep your elbows near your sides. Extend your arms against the cable, then return slowly without moving your upper arms." },
            new ExerciseDefinition { Id = -6, Name = "Overhead Tricep Ext", PrimaryMuscle = MuscleGroup.Triceps, SecondaryMuscles = "Shoulders", Equipment = "Dumbbell", MovementType = "Elbow extension", Instructions = "Hold one weight overhead with elbows bent. Extend your elbows to raise the weight, then lower it behind your head with control." },
            new ExerciseDefinition { Id = -7, Name = "Conventional Deadlift", PrimaryMuscle = MuscleGroup.Back, SecondaryMuscles = "Glutes, hamstrings, quads, core", Equipment = "Barbell", MovementType = "Hip hinge", Instructions = "Stand with the bar over midfoot. Brace, hinge and bend to grip the bar, then stand by pushing through the floor while keeping the bar close." },
            new ExerciseDefinition { Id = -8, Name = "Lat Pulldown", PrimaryMuscle = MuscleGroup.Back, SecondaryMuscles = "Biceps, rear delts", Equipment = "Cable", MovementType = "Vertical pull", Instructions = "Grip the bar wider than shoulder width. Pull it toward your upper chest while keeping your torso steady, then return with control." },
            new ExerciseDefinition { Id = -9, Name = "Barbell Row", PrimaryMuscle = MuscleGroup.Back, SecondaryMuscles = "Biceps, rear delts, core", Equipment = "Barbell", MovementType = "Horizontal pull", Instructions = "Hinge forward with a braced, neutral torso. Pull the bar toward your lower ribs, then lower it without changing your torso angle." },
            new ExerciseDefinition { Id = -10, Name = "Cable Row", PrimaryMuscle = MuscleGroup.Back, SecondaryMuscles = "Biceps, rear delts", Equipment = "Cable", MovementType = "Horizontal pull", Instructions = "Sit tall and brace. Pull the handle toward your torso by driving your elbows back, then extend your arms without rounding your back." },
            new ExerciseDefinition { Id = -11, Name = "Dumbbell Curl", PrimaryMuscle = MuscleGroup.Biceps, SecondaryMuscles = "Forearms", Equipment = "Dumbbell", MovementType = "Elbow flexion", Instructions = "Keep your upper arms still and curl the weights toward your shoulders. Lower them slowly to the starting position." },
            new ExerciseDefinition { Id = -12, Name = "Hammer Curl", PrimaryMuscle = MuscleGroup.Biceps, SecondaryMuscles = "Forearms", Equipment = "Dumbbell", MovementType = "Elbow flexion", Instructions = "Hold dumbbells with palms facing each other. Curl without swinging, then lower under control." },
            new ExerciseDefinition { Id = -13, Name = "Face Pulls", PrimaryMuscle = MuscleGroup.RearDelts, SecondaryMuscles = "Upper back, external rotators", Equipment = "Cable", MovementType = "Horizontal pull", Instructions = "Set the cable around face height. Pull the rope toward your face with elbows high, then return while keeping your shoulders controlled." },
            new ExerciseDefinition { Id = -14, Name = "Tricep Dips", PrimaryMuscle = MuscleGroup.Triceps, SecondaryMuscles = "Chest, shoulders", Equipment = "Dip bars", MovementType = "Vertical push", Instructions = "Support yourself on the bars and lower by bending your elbows to a comfortable depth. Press back up without shrugging." },
            new ExerciseDefinition { Id = -15, Name = "Weighted Pull-ups", PrimaryMuscle = MuscleGroup.Back, SecondaryMuscles = "Biceps, forearms", Equipment = "Pull-up bar, weight", MovementType = "Vertical pull", Instructions = "Use a secure added load if needed. Pull until your chin clears the bar, then lower to a controlled hang." },
            new ExerciseDefinition { Id = -16, Name = "Back Squat", PrimaryMuscle = MuscleGroup.Quads, SecondaryMuscles = "Glutes, hamstrings, core", Equipment = "Barbell, rack", MovementType = "Squat", Instructions = "Set the bar securely across your upper back. Brace, sit between your hips to a controlled depth, then stand while keeping your feet planted." },
            new ExerciseDefinition { Id = -17, Name = "Romanian Deadlift", PrimaryMuscle = MuscleGroup.Hamstrings, SecondaryMuscles = "Glutes, back", Equipment = "Barbell", MovementType = "Hip hinge", Instructions = "Hold the bar against your thighs. Push your hips back with a slight knee bend, lower to a strong hamstring stretch, then stand by extending your hips." },
            new ExerciseDefinition { Id = -18, Name = "Leg Press", PrimaryMuscle = MuscleGroup.Quads, SecondaryMuscles = "Glutes, hamstrings", Equipment = "Leg press machine", MovementType = "Squat", Instructions = "Set your feet comfortably on the platform. Lower the sled under control to a pain-free depth, then press through your feet without locking your knees." },
            new ExerciseDefinition { Id = -19, Name = "Leg Curl", PrimaryMuscle = MuscleGroup.Hamstrings, SecondaryMuscles = "Calves", Equipment = "Leg curl machine", MovementType = "Knee flexion", Instructions = "Align your knees with the machine pivot. Curl the pad toward you, pause briefly, then return slowly." },
            new ExerciseDefinition { Id = -20, Name = "Standing Calf Raise", PrimaryMuscle = MuscleGroup.Calves, SecondaryMuscles = "Soleus", Equipment = "Calf raise machine", MovementType = "Plantar flexion", Instructions = "Lower your heels to a comfortable stretch, then rise onto the balls of your feet. Use a controlled range without bouncing." },
            new ExerciseDefinition { Id = -21, Name = "Hack Squat", PrimaryMuscle = MuscleGroup.Quads, SecondaryMuscles = "Glutes", Equipment = "Hack squat machine", MovementType = "Squat", Instructions = "Set your shoulders against the pads and feet securely on the platform. Lower to a controlled depth, then press back up smoothly." },
            new ExerciseDefinition { Id = -22, Name = "Bulgarian Split Squat", PrimaryMuscle = MuscleGroup.Glutes, SecondaryMuscles = "Quads, hamstrings", Equipment = "Dumbbell, bench", MovementType = "Single-leg squat", Instructions = "Place your rear foot on a stable bench. Lower under control by bending the front leg, then drive through the front foot to stand." },
            new ExerciseDefinition { Id = -23, Name = "Leg Extension", PrimaryMuscle = MuscleGroup.Quads, SecondaryMuscles = string.Empty, Equipment = "Leg extension machine", MovementType = "Knee extension", Instructions = "Align your knees with the machine pivot. Extend your knees smoothly, then lower the pad under control." },
            new ExerciseDefinition { Id = -24, Name = "Seated Leg Curl", PrimaryMuscle = MuscleGroup.Hamstrings, SecondaryMuscles = "Calves", Equipment = "Leg curl machine", MovementType = "Knee flexion", Instructions = "Adjust the pads so your knees align with the pivot. Curl the lower pad down and back, then return slowly." },
            new ExerciseDefinition { Id = -25, Name = "Incline Barbell Bench", PrimaryMuscle = MuscleGroup.Chest, SecondaryMuscles = "Triceps, shoulders", Equipment = "Barbell, incline bench", MovementType = "Incline push", Instructions = "Set the bench to a low incline. Lower the bar under control to the upper chest, then press to full extension." },
            new ExerciseDefinition { Id = -26, Name = "Cable Fly", PrimaryMuscle = MuscleGroup.Chest, SecondaryMuscles = "Shoulders", Equipment = "Cable", MovementType = "Horizontal adduction", Instructions = "Stand between the pulleys with a slight elbow bend. Bring your hands together in front of your chest, then return until you feel a comfortable stretch." },
            new ExerciseDefinition { Id = -27, Name = "Dips", PrimaryMuscle = MuscleGroup.Triceps, SecondaryMuscles = "Chest, shoulders", Equipment = "Dip bars", MovementType = "Vertical push", Instructions = "Support yourself on the bars and lower by bending your elbows to a comfortable depth. Press back up without shrugging." },
            new ExerciseDefinition { Id = -28, Name = "T-Bar Row", PrimaryMuscle = MuscleGroup.Back, SecondaryMuscles = "Biceps, rear delts", Equipment = "T-bar row machine", MovementType = "Horizontal pull", Instructions = "Brace your torso and pull the handle toward your abdomen. Lower the load with control while keeping your back stable." },
            new ExerciseDefinition { Id = -29, Name = "Incline DB Curl", PrimaryMuscle = MuscleGroup.Biceps, SecondaryMuscles = "Forearms", Equipment = "Dumbbell, incline bench", MovementType = "Elbow flexion", Instructions = "Sit against an incline bench with arms hanging. Curl the dumbbells without swinging, then lower fully under control." },
            new ExerciseDefinition { Id = -31, Name = "Seated Calf Raise", PrimaryMuscle = MuscleGroup.Calves, SecondaryMuscles = "Soleus", Equipment = "Seated calf raise machine", MovementType = "Plantar flexion", Instructions = "Lower your heels under control, then raise them as high as comfortable. Pause briefly and avoid bouncing." },
            new ExerciseDefinition { Id = -32, Name = "Dumbbell Row", PrimaryMuscle = MuscleGroup.Back, SecondaryMuscles = "Biceps, rear delts", Equipment = "Dumbbell, bench", MovementType = "Horizontal pull", Instructions = "Support one hand and knee on a bench. Pull the dumbbell toward your hip while keeping your torso steady, then lower it under control." },
            new ExerciseDefinition { Id = -34, Name = "Chin-Ups", PrimaryMuscle = MuscleGroup.Back, SecondaryMuscles = "Biceps, forearms", Equipment = "Pull-up bar", MovementType = "Vertical pull", Instructions = "Use an underhand grip. Pull until your chin clears the bar, then lower to a controlled hang." },
            new ExerciseDefinition { Id = -35, Name = "Ab Wheel", PrimaryMuscle = MuscleGroup.Core, SecondaryMuscles = "Shoulders, lats", Equipment = "Ab wheel", MovementType = "Anti-extension", Instructions = "Kneel with the wheel under your shoulders. Brace your trunk and roll forward only as far as you can control, then return without arching your back." },
            new ExerciseDefinition { Id = -36, Name = "Hanging Leg Raise", PrimaryMuscle = MuscleGroup.Core, SecondaryMuscles = "Hip flexors, lats", Equipment = "Pull-up bar", MovementType = "Hip flexion", Instructions = "Hang from a stable bar and brace. Raise your knees or straight legs without swinging, then lower slowly." },
            new ExerciseDefinition { Id = -37, Name = "DB Shoulder Press", PrimaryMuscle = MuscleGroup.Shoulders, SecondaryMuscles = "Triceps, upper chest", Equipment = "Dumbbell, bench", MovementType = "Vertical push", Instructions = "Start with dumbbells at shoulder height. Brace and press overhead, then lower them under control." },
            new ExerciseDefinition { Id = -38, Name = "Barbell Curl", PrimaryMuscle = MuscleGroup.Biceps, SecondaryMuscles = "Forearms", Equipment = "Barbell", MovementType = "Elbow flexion", Instructions = "Stand tall with the bar at arm's length. Curl toward your shoulders without swinging, then lower under control." },
            new ExerciseDefinition { Id = -39, Name = "Dumbbell Bench Press", PrimaryMuscle = MuscleGroup.Chest, SecondaryMuscles = "Triceps, shoulders", Equipment = "Dumbbell, flat bench", MovementType = "Horizontal push", Instructions = "Lie on a flat bench with dumbbells over your chest. Lower them beside your chest, then press up smoothly." },
            new ExerciseDefinition { Id = -41, Name = "Close-Grip Bench Press", PrimaryMuscle = MuscleGroup.Triceps, SecondaryMuscles = "Chest, shoulders", Equipment = "Barbell, bench", MovementType = "Horizontal push", Instructions = "Use a comfortable grip narrower than your regular bench press. Lower the bar under control and press while keeping wrists stacked over elbows." },
            new ExerciseDefinition { Id = -42, Name = "Push-Up", PrimaryMuscle = MuscleGroup.Chest, SecondaryMuscles = "Triceps, shoulders, core", Equipment = "Bodyweight", MovementType = "Horizontal push", Instructions = "Keep your body in a straight line. Lower your chest toward the floor with elbows controlled, then press back to the starting position." },
            new ExerciseDefinition { Id = -43, Name = "Machine Chest Press", PrimaryMuscle = MuscleGroup.Chest, SecondaryMuscles = "Triceps, shoulders", Equipment = "Chest press machine", MovementType = "Horizontal push", Instructions = "Adjust the seat so the handles are near chest level. Press forward without locking your elbows, then return slowly." },
            new ExerciseDefinition { Id = -44, Name = "Pec Deck", PrimaryMuscle = MuscleGroup.Chest, SecondaryMuscles = "Shoulders", Equipment = "Pec deck machine", MovementType = "Horizontal adduction", Instructions = "Set the seat so your elbows align with the machine pivot. Bring your arms together with a slight bend, then return under control." },
            new ExerciseDefinition { Id = -45, Name = "Dumbbell Fly", PrimaryMuscle = MuscleGroup.Chest, SecondaryMuscles = "Shoulders", Equipment = "Dumbbell, flat bench", MovementType = "Horizontal adduction", Instructions = "Lie on a bench with a slight bend in your elbows. Open your arms to a comfortable stretch, then bring the weights together without changing the elbow bend." },
            new ExerciseDefinition { Id = -46, Name = "Reverse Fly", PrimaryMuscle = MuscleGroup.RearDelts, SecondaryMuscles = "Upper back", Equipment = "Dumbbell", MovementType = "Shoulder horizontal abduction", Instructions = "Hinge forward with a steady torso. Raise the weights out to the sides with a slight elbow bend, then lower slowly." },
            new ExerciseDefinition { Id = -47, Name = "Reverse Pec Deck", PrimaryMuscle = MuscleGroup.RearDelts, SecondaryMuscles = "Upper back", Equipment = "Pec deck machine", MovementType = "Shoulder horizontal abduction", Instructions = "Face the machine pads and hold the handles. Move your arms outward with control, then return without letting the weights drop." },
            new ExerciseDefinition { Id = -48, Name = "Cable Lateral Raise", PrimaryMuscle = MuscleGroup.Shoulders, SecondaryMuscles = "Traps", Equipment = "Cable", MovementType = "Shoulder isolation", Instructions = "Stand beside a low cable with the handle in your outside hand. Raise your arm out to shoulder height, then lower slowly." },
            new ExerciseDefinition { Id = -49, Name = "Front Raise", PrimaryMuscle = MuscleGroup.Shoulders, SecondaryMuscles = "Upper chest", Equipment = "Dumbbell", MovementType = "Shoulder flexion", Instructions = "Raise the weights in front of you to about shoulder height with elbows slightly bent, then lower under control." },
            new ExerciseDefinition { Id = -51, Name = "Upright Row", PrimaryMuscle = MuscleGroup.Shoulders, SecondaryMuscles = "Traps, biceps", Equipment = "Barbell", MovementType = "Shoulder abduction", Instructions = "Use a comfortable grip and raise the bar in front of your torso only as high as your shoulders tolerate. Lower with control." },
            new ExerciseDefinition { Id = -52, Name = "Dumbbell Shrug", PrimaryMuscle = MuscleGroup.Back, SecondaryMuscles = "Traps, forearms", Equipment = "Dumbbell", MovementType = "Scapular elevation", Instructions = "Stand tall holding dumbbells at your sides. Raise your shoulders toward your ears without rolling them, then lower slowly." },
            new ExerciseDefinition { Id = -53, Name = "Pull-Up", PrimaryMuscle = MuscleGroup.Back, SecondaryMuscles = "Biceps, forearms", Equipment = "Pull-up bar", MovementType = "Vertical pull", Instructions = "Use an overhand grip. Pull until your chin clears the bar, then lower to a controlled hang." },
            new ExerciseDefinition { Id = -54, Name = "Chest-Supported Row", PrimaryMuscle = MuscleGroup.Back, SecondaryMuscles = "Biceps, rear delts", Equipment = "Dumbbell, incline bench", MovementType = "Horizontal pull", Instructions = "Lie chest-down on an incline bench. Row the weights toward your torso without lifting your chest, then lower slowly." },
            new ExerciseDefinition { Id = -56, Name = "Single-Arm Cable Row", PrimaryMuscle = MuscleGroup.Back, SecondaryMuscles = "Biceps, rear delts", Equipment = "Cable", MovementType = "Horizontal pull", Instructions = "Face a cable handle and brace your torso. Pull one elbow back toward your side, then return with control." },
            new ExerciseDefinition { Id = -57, Name = "Close-Grip Lat Pulldown", PrimaryMuscle = MuscleGroup.Back, SecondaryMuscles = "Biceps, rear delts", Equipment = "Cable, close-grip handle", MovementType = "Vertical pull", Instructions = "Sit with thighs secured and hold a close-grip handle. Pull toward your upper chest while keeping your torso steady, then return slowly." },
            new ExerciseDefinition { Id = -58, Name = "Straight-Arm Pulldown", PrimaryMuscle = MuscleGroup.Back, SecondaryMuscles = "Triceps, core", Equipment = "Cable", MovementType = "Shoulder extension", Instructions = "Stand facing a high cable with arms nearly straight. Pull the handle toward your thighs while keeping your torso still, then return slowly." },
            new ExerciseDefinition { Id = -59, Name = "Preacher Curl", PrimaryMuscle = MuscleGroup.Biceps, SecondaryMuscles = "Forearms", Equipment = "Preacher bench, barbell", MovementType = "Elbow flexion", Instructions = "Rest your upper arms on the pad. Curl the weight without lifting your elbows, then lower until your arms are nearly straight." },
            new ExerciseDefinition { Id = -60, Name = "Concentration Curl", PrimaryMuscle = MuscleGroup.Biceps, SecondaryMuscles = "Forearms", Equipment = "Dumbbell, bench", MovementType = "Elbow flexion", Instructions = "Sit and brace your upper arm against your inner thigh. Curl the dumbbell toward your shoulder, then lower slowly." },
            new ExerciseDefinition { Id = -61, Name = "Wrist Curl", PrimaryMuscle = MuscleGroup.Biceps, SecondaryMuscles = "Forearms", Equipment = "Dumbbell", MovementType = "Wrist flexion", Instructions = "Rest your forearms on your thighs with palms up. Curl your wrists to raise the weights, then lower through a comfortable range." },
            new ExerciseDefinition { Id = -62, Name = "Skull Crusher", PrimaryMuscle = MuscleGroup.Triceps, SecondaryMuscles = "Shoulders", Equipment = "EZ bar, bench", MovementType = "Elbow extension", Instructions = "Lie on a bench and hold the bar above your shoulders. Bend your elbows to lower it near your forehead, then extend them to return." },
            new ExerciseDefinition { Id = -63, Name = "Overhead Cable Tricep Extension", PrimaryMuscle = MuscleGroup.Triceps, SecondaryMuscles = "Shoulders", Equipment = "Cable, rope attachment", MovementType = "Elbow extension", Instructions = "Face away from a low cable with the rope behind your head. Extend your elbows overhead, then return slowly while keeping your upper arms steady." },
            new ExerciseDefinition { Id = -64, Name = "Close-Grip Push-Up", PrimaryMuscle = MuscleGroup.Triceps, SecondaryMuscles = "Chest, shoulders, core", Equipment = "Bodyweight", MovementType = "Horizontal push", Instructions = "Set your hands slightly narrower than shoulder width. Keep your body straight, lower with elbows near your sides, then press up." },
            new ExerciseDefinition { Id = -65, Name = "Front Squat", PrimaryMuscle = MuscleGroup.Quads, SecondaryMuscles = "Glutes, core, upper back", Equipment = "Barbell, rack", MovementType = "Squat", Instructions = "Rest the bar across the front of your shoulders and keep your elbows lifted. Brace, squat to a controlled depth, then stand while keeping your torso upright." },
            new ExerciseDefinition { Id = -66, Name = "Goblet Squat", PrimaryMuscle = MuscleGroup.Quads, SecondaryMuscles = "Glutes, core", Equipment = "Dumbbell or kettlebell", MovementType = "Squat", Instructions = "Hold a weight close to your chest. Squat between your hips with your torso braced, then stand while keeping your feet planted." },
            new ExerciseDefinition { Id = -67, Name = "Sumo Deadlift", PrimaryMuscle = MuscleGroup.Glutes, SecondaryMuscles = "Quads, hamstrings, back", Equipment = "Barbell", MovementType = "Hip hinge", Instructions = "Take a wide stance with toes turned slightly out and grip the bar inside your legs. Brace and push the floor apart as you stand, then lower with control." },
            new ExerciseDefinition { Id = -68, Name = "Trap Bar Deadlift", PrimaryMuscle = MuscleGroup.Glutes, SecondaryMuscles = "Quads, hamstrings, back", Equipment = "Trap bar", MovementType = "Hip hinge", Instructions = "Stand centered in the trap bar and grip the handles. Brace, push through the floor to stand tall, then lower the bar under control." },
            new ExerciseDefinition { Id = -69, Name = "Good Morning", PrimaryMuscle = MuscleGroup.Hamstrings, SecondaryMuscles = "Glutes, back", Equipment = "Barbell, rack", MovementType = "Hip hinge", Instructions = "With a secure bar position and soft knees, brace and hinge your hips back while keeping your spine neutral. Stand by extending your hips." },
            new ExerciseDefinition { Id = -70, Name = "Barbell Hip Thrust", PrimaryMuscle = MuscleGroup.Glutes, SecondaryMuscles = "Hamstrings, core", Equipment = "Barbell, bench", MovementType = "Hip extension", Instructions = "Rest your upper back against a stable bench with the bar over your hips. Drive through your feet to extend your hips, then lower under control." },
            new ExerciseDefinition { Id = -71, Name = "Glute Bridge", PrimaryMuscle = MuscleGroup.Glutes, SecondaryMuscles = "Hamstrings, core", Equipment = "Bodyweight", MovementType = "Hip extension", Instructions = "Lie on your back with knees bent and feet planted. Brace and raise your hips until your body forms a straight line from shoulders to knees, then lower slowly." },
            new ExerciseDefinition { Id = -72, Name = "Walking Lunge", PrimaryMuscle = MuscleGroup.Quads, SecondaryMuscles = "Glutes, hamstrings", Equipment = "Bodyweight or dumbbells", MovementType = "Single-leg squat", Instructions = "Step forward into a controlled lunge, lowering until both knees are comfortably bent. Push through the front foot to step into the next lunge." },
            new ExerciseDefinition { Id = -73, Name = "Reverse Lunge", PrimaryMuscle = MuscleGroup.Glutes, SecondaryMuscles = "Quads, hamstrings", Equipment = "Bodyweight or dumbbells", MovementType = "Single-leg squat", Instructions = "Step one foot back and lower under control. Push through the front foot to return to standing, then alternate sides." },
            new ExerciseDefinition { Id = -74, Name = "Step-Up", PrimaryMuscle = MuscleGroup.Quads, SecondaryMuscles = "Glutes, hamstrings", Equipment = "Bench or box, dumbbells optional", MovementType = "Single-leg squat", Instructions = "Place one foot fully on a stable step. Drive through that foot to stand on the step, then lower slowly and repeat on the other side." },
            new ExerciseDefinition { Id = -75, Name = "Single-Leg Romanian Deadlift", PrimaryMuscle = MuscleGroup.Hamstrings, SecondaryMuscles = "Glutes, core", Equipment = "Dumbbell", MovementType = "Single-leg hip hinge", Instructions = "Balance on one leg with a soft knee. Hinge at the hip as the free leg reaches back, then return upright without twisting." },
            new ExerciseDefinition { Id = -76, Name = "Hip Abduction", PrimaryMuscle = MuscleGroup.Glutes, SecondaryMuscles = "Hip abductors", Equipment = "Hip abduction machine", MovementType = "Hip abduction", Instructions = "Sit with knees against the pads. Move your knees outward against the resistance, pause briefly, then return slowly." },
            new ExerciseDefinition { Id = -77, Name = "Hip Adduction", PrimaryMuscle = MuscleGroup.Glutes, SecondaryMuscles = "Inner thighs", Equipment = "Hip adduction machine", MovementType = "Hip adduction", Instructions = "Sit with legs set against the pads. Bring your legs together against resistance, then return to the start under control." },
            new ExerciseDefinition { Id = -80, Name = "Cable Crunch", PrimaryMuscle = MuscleGroup.Core, SecondaryMuscles = "Obliques", Equipment = "Cable, rope attachment", MovementType = "Trunk flexion", Instructions = "Kneel facing a high cable and hold the rope by your head. Curl your ribs toward your pelvis, then return without pulling with your arms." },
            new ExerciseDefinition { Id = -81, Name = "Plank", PrimaryMuscle = MuscleGroup.Core, SecondaryMuscles = "Shoulders, glutes", Equipment = "Bodyweight", MovementType = "Anti-extension", Instructions = "Support yourself on your forearms and toes. Brace your trunk and keep your body in a straight line without letting your hips sag." },
            new ExerciseDefinition { Id = -82, Name = "Side Plank", PrimaryMuscle = MuscleGroup.Core, SecondaryMuscles = "Obliques, shoulders, glutes", Equipment = "Bodyweight", MovementType = "Anti-lateral flexion", Instructions = "Support yourself on one forearm and the side of your foot. Lift your hips and keep your body aligned, then switch sides." },
            new ExerciseDefinition { Id = -83, Name = "Hanging Knee Raise", PrimaryMuscle = MuscleGroup.Core, SecondaryMuscles = "Hip flexors, lats", Equipment = "Pull-up bar", MovementType = "Hip flexion", Instructions = "Hang from a stable bar and brace. Raise your knees toward your torso without swinging, then lower slowly." },
            new ExerciseDefinition { Id = -84, Name = "Pallof Press", PrimaryMuscle = MuscleGroup.Core, SecondaryMuscles = "Shoulders, glutes", Equipment = "Cable or resistance band", MovementType = "Anti-rotation", Instructions = "Stand sideways to a cable or band and hold it at your chest. Press your hands forward while resisting rotation, then return and switch sides." }
        );
    }
}
