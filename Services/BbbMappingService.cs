using FiveThreeOneTracker.Models;

namespace FiveThreeOneTracker.Services;

public interface IBbbMappingService
{
    LiftType GetBbbLiftType(LiftType mainLiftType, BbbMode mode);
    string GetBbbLiftName(LiftType mainLiftType, BbbMode mode);
    IReadOnlyDictionary<LiftType, LiftType> GetMappings(BbbMode mode);
}

public class BbbMappingService : IBbbMappingService
{
    private static readonly Dictionary<LiftType, LiftType> OppositeDayMappings = new()
    {
        [LiftType.BenchPress] = LiftType.OverheadPress,
        [LiftType.OverheadPress] = LiftType.BenchPress,
        [LiftType.Squat] = LiftType.Deadlift,
        [LiftType.Deadlift] = LiftType.Squat
    };

    private static readonly Dictionary<LiftType, LiftType> SameDayMappings = new()
    {
        [LiftType.BenchPress] = LiftType.BenchPress,
        [LiftType.OverheadPress] = LiftType.OverheadPress,
        [LiftType.Squat] = LiftType.Squat,
        [LiftType.Deadlift] = LiftType.Deadlift
    };

    private static readonly Dictionary<LiftType, string> LiftNames = new()
    {
        [LiftType.Squat] = "Squat",
        [LiftType.BenchPress] = "Bench Press",
        [LiftType.Deadlift] = "Deadlift",
        [LiftType.OverheadPress] = "Overhead Press"
    };

    public LiftType GetBbbLiftType(LiftType mainLiftType, BbbMode mode)
    {
        var mappings = mode == BbbMode.OppositeDay ? OppositeDayMappings : SameDayMappings;
        return mappings[mainLiftType];
    }

    public string GetBbbLiftName(LiftType mainLiftType, BbbMode mode)
    {
        var bbbType = GetBbbLiftType(mainLiftType, mode);
        return LiftNames[bbbType];
    }

    public IReadOnlyDictionary<LiftType, LiftType> GetMappings(BbbMode mode)
    {
        return mode == BbbMode.OppositeDay ? OppositeDayMappings : SameDayMappings;
    }
}
