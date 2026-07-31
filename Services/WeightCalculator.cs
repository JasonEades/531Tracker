using FiveThreeOneTracker.Models;

namespace FiveThreeOneTracker.Services;

public interface IWeightCalculator
{
    double RoundToNearest5(double weight);
    IReadOnlyList<(double Percentage, int Reps)> GetMainSets(WeekNumber week);
    IReadOnlyList<(double Percentage, int Reps)> GetFivesProMainSets(WeekNumber week);
    IReadOnlyList<(double Percentage, int Reps)> GetWarmupSets();
    double CalculateWeight(double trainingMax, double percentage);
    double CalculateBbbWeight(double trainingMax, double bbbPercentage);
    double CalculateEstimated1RM(double weight, int reps);
}

public class WeightCalculator : IWeightCalculator
{
    private static readonly Dictionary<WeekNumber, (double Percentage, int Reps)[]> WeekSchemes = new()
    {
        [WeekNumber.Week1] = [(0.65, 5), (0.75, 5), (0.85, 5)],
        [WeekNumber.Week2] = [(0.70, 3), (0.80, 3), (0.90, 3)],
        [WeekNumber.Week3] = [(0.75, 5), (0.85, 3), (0.95, 1)],
        [WeekNumber.Week4] = [(0.40, 5), (0.50, 5), (0.60, 5)]
    };

    // 5s PRO: same percentages, all sets are straight 5s (no AMRAP)
    private static readonly Dictionary<WeekNumber, (double Percentage, int Reps)[]> FivesProWeekSchemes = new()
    {
        [WeekNumber.Week1] = [(0.65, 5), (0.75, 5), (0.85, 5)],
        [WeekNumber.Week2] = [(0.70, 5), (0.80, 5), (0.90, 5)],
        [WeekNumber.Week3] = [(0.75, 5), (0.85, 5), (0.95, 5)],
        [WeekNumber.Week4] = [(0.40, 5), (0.50, 5), (0.60, 5)]
    };

    public double RoundToNearest5(double weight)
    {
        return Math.Round(weight / 5.0) * 5.0;
    }

    public IReadOnlyList<(double Percentage, int Reps)> GetMainSets(WeekNumber week)
    {
        return WeekSchemes[week];
    }

    public IReadOnlyList<(double Percentage, int Reps)> GetFivesProMainSets(WeekNumber week)
    {
        return FivesProWeekSchemes[week];
    }

    private static readonly (double Percentage, int Reps)[] WarmupScheme = [(0.40, 5), (0.50, 5)];

    public IReadOnlyList<(double Percentage, int Reps)> GetWarmupSets()
    {
        return WarmupScheme;
    }

    public double CalculateWeight(double trainingMax, double percentage)
    {
        return RoundToNearest5(trainingMax * percentage);
    }

    public double CalculateBbbWeight(double trainingMax, double bbbPercentage)
    {
        return RoundToNearest5(trainingMax * (bbbPercentage / 100.0));
    }

    // Epley formula: weight × (1 + reps / 30), rounded to nearest 5
    public double CalculateEstimated1RM(double weight, int reps)
    {
        if (reps <= 0) return 0;
        if (reps == 1) return weight;
        return RoundToNearest5(weight * (1.0 + reps / 30.0));
    }
}
