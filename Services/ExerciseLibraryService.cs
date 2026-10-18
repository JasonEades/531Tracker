using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public interface IExerciseLibraryService
{
    Task<List<ExerciseDefinition>> GetExercisesAsync(string? search = null, MuscleGroup? muscle = null, string? equipment = null);
    Task<ExerciseDefinition?> GetExerciseAsync(int id);
    Task<ExerciseDefinition> CreateCustomExerciseAsync(
        string name,
        MuscleGroup primaryMuscle,
        string secondaryMuscles,
        string equipment,
        string movementType,
        string instructions);
}

public sealed class ExerciseLibraryService(AppDbContext db, ICurrentUserService userContext) : IExerciseLibraryService
{
    public async Task<List<ExerciseDefinition>> GetExercisesAsync(
        string? search = null,
        MuscleGroup? muscle = null,
        string? equipment = null)
    {
        var userId = await userContext.GetUserIdAsync();
        var query = db.ExerciseDefinitions
            .AsNoTracking()
            .Where(exercise => exercise.UserId == null || exercise.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(exercise =>
                exercise.Name.ToLower().Contains(term)
                || exercise.SecondaryMuscles.ToLower().Contains(term)
                || exercise.Equipment.ToLower().Contains(term)
                || exercise.MovementType.ToLower().Contains(term));
        }

        if (muscle is not null)
            query = query.Where(exercise => exercise.PrimaryMuscle == muscle);
        if (!string.IsNullOrWhiteSpace(equipment))
            query = query.Where(exercise => exercise.Equipment == equipment);

        return await query.OrderBy(exercise => exercise.Name).ToListAsync();
    }

    public async Task<ExerciseDefinition?> GetExerciseAsync(int id)
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.ExerciseDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(exercise => exercise.Id == id && (exercise.UserId == null || exercise.UserId == userId));
    }

    public async Task<ExerciseDefinition> CreateCustomExerciseAsync(
        string name,
        MuscleGroup primaryMuscle,
        string secondaryMuscles,
        string equipment,
        string movementType,
        string instructions)
    {
        var userId = await userContext.GetUserIdAsync();
        name = name.Trim();
        secondaryMuscles = secondaryMuscles.Trim();
        equipment = equipment.Trim();
        movementType = movementType.Trim();
        instructions = instructions.Trim();

        if (name.Length is < 1 or > 100
            || secondaryMuscles.Length > 200
            || equipment.Length > 100
            || movementType.Length > 60
            || instructions.Length is < 1 or > 2000
            || !Enum.IsDefined(primaryMuscle))
            throw new ArgumentException("Enter an exercise name, valid muscle group, and instructions within the listed limits.");

        var exercise = new ExerciseDefinition
        {
            Name = name,
            PrimaryMuscle = primaryMuscle,
            SecondaryMuscles = secondaryMuscles,
            Equipment = equipment,
            MovementType = movementType,
            Instructions = instructions,
            UserId = userId
        };
        db.ExerciseDefinitions.Add(exercise);
        await db.SaveChangesAsync();
        return exercise;
    }
}
