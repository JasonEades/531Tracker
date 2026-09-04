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
    public DbSet<AccessoryHistory> AccessoryHistory => Set<AccessoryHistory>();
    public DbSet<UserEquipment> UserEquipment => Set<UserEquipment>();
    public DbSet<PlateInventory> PlateInventory => Set<PlateInventory>();
    public DbSet<Bar> Bars => Set<Bar>();

    // ── PPL ──────────────────────────────────────────────────────────────────
    public DbSet<PplProgram> PplPrograms => Set<PplProgram>();
    public DbSet<PplDayTemplate> PplDayTemplates => Set<PplDayTemplate>();
    public DbSet<PplExerciseSlot> PplExerciseSlots => Set<PplExerciseSlot>();
    public DbSet<PplSession> PplSessions => Set<PplSession>();
    public DbSet<PplSessionExercise> PplSessionExercises => Set<PplSessionExercise>();
    public DbSet<PplSessionSet> PplSessionSets => Set<PplSessionSet>();

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
            entity.Property(e => e.BbbMode).HasConversion<string>();
            entity.HasMany(c => c.Weeks)
                  .WithOne(w => w.Cycle)
                  .HasForeignKey(w => w.CycleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Week>(entity =>
        {
            entity.Property(e => e.WeekNumber).HasConversion<int>();
            entity.HasMany(w => w.Workouts)
                  .WithOne(wo => wo.Week)
                  .HasForeignKey(wo => wo.WeekId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Workout>(entity =>
        {
            entity.Property(e => e.MainLiftType).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
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
        });

        modelBuilder.Entity<WorkoutAccessory>(entity =>
        {
            entity.HasOne(wa => wa.Accessory)
                  .WithMany(a => a.WorkoutAccessories)
                  .HasForeignKey(wa => wa.AccessoryId);
        });

        modelBuilder.Entity<AccessoryHistory>(entity =>
        {
            entity.HasIndex(ah => new { ah.UserId, ah.AccessoryId, ah.RecordedAt });
            entity.HasOne(ah => ah.Accessory)
                  .WithMany(a => a.History)
                  .HasForeignKey(ah => ah.AccessoryId)
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
        });

        modelBuilder.Entity<PplDayTemplate>(entity =>
        {
            entity.Property(e => e.DayType).HasConversion<string>();
            entity.Property(e => e.Variant).HasConversion<string>();
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
            entity.HasOne(e => e.Lift)
                  .WithMany()
                  .HasForeignKey(e => e.LiftId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PplSession>(entity =>
        {
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasMany(s => s.Exercises)
                  .WithOne(e => e.Session)
                  .HasForeignKey(e => e.PplSessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PplSessionExercise>(entity =>
        {
            entity.HasOne(e => e.ExerciseSlot)
                  .WithMany(s => s.SessionExercises)
                  .HasForeignKey(e => e.PplExerciseSlotId)
                  .OnDelete(DeleteBehavior.Restrict);
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
            new Accessory { Id = 1, Name = "Barbell Row", Description = "Bent-over barbell row" },
            new Accessory { Id = 2, Name = "Dumbbell Row", Description = "Single-arm dumbbell row" },
            new Accessory { Id = 3, Name = "Lat Pulldown", Description = "Cable lat pulldown" },
            new Accessory { Id = 4, Name = "Face Pull", Description = "Cable face pull" },
            new Accessory { Id = 5, Name = "Dips", Description = "Parallel bar dips" },
            new Accessory { Id = 6, Name = "Chin-Ups", Description = "Chin-ups / pull-ups" },
            new Accessory { Id = 7, Name = "Leg Curl", Description = "Lying or seated leg curl" },
            new Accessory { Id = 8, Name = "Leg Press", Description = "Machine leg press" },
            new Accessory { Id = 9, Name = "Ab Wheel", Description = "Ab wheel rollout" },
            new Accessory { Id = 10, Name = "Hanging Leg Raise", Description = "Hanging leg raise" },
            new Accessory { Id = 11, Name = "Dumbbell Curl", Description = "Standing dumbbell curl" },
            new Accessory { Id = 12, Name = "Tricep Pushdown", Description = "Cable tricep pushdown" },
            new Accessory { Id = 13, Name = "Lateral Raise", Description = "Dumbbell lateral raise" },
            new Accessory { Id = 14, Name = "Romanian Deadlift", Description = "Romanian deadlift" },
            new Accessory { Id = 15, Name = "Bulgarian Split Squat", Description = "Rear foot elevated split squat" }
        );
    }
}
