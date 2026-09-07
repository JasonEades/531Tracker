using FiveThreeOneTracker.Models;
using FiveThreeOneTracker.Services;
using Xunit;

namespace WorkoutExportTests;

public sealed class DashboardAnalyticsTests
{
    [Theory]
    [InlineData(225, 5, 260)]
    [InlineData(275, 1, 275)]
    [InlineData(100, 10, 135)]
    public void EstimatedOneRepMaxUsesExistingEpleyFormula(double weight, int reps, double expected)
    {
        var calculator = new WeightCalculator();

        Assert.Equal(expected, calculator.CalculateEstimated1RM(weight, reps));
    }

    [Fact]
    public void CycleCompletionUsesProgrammedSessionsOnly()
    {
        var analytics = new CurrentProgramAnalytics
        {
            PlannedSessions = 16,
            CompletedSessions = 10,
            AdditionalSessions = 8
        };

        Assert.Equal(63, analytics.CompletionPercent);
        Assert.Equal(8, analytics.AdditionalSessions);
    }

    [Fact]
    public void EmptyCycleHasZeroCompletionInsteadOfFabricatingProgress()
    {
        var analytics = new CurrentProgramAnalytics();

        Assert.Equal(0, analytics.CompletionPercent);
    }

    [Fact]
    public void ConnectedStepAnalyticsProvidesThirtyDays()
    {
        var analytics = new StepAnalytics
        {
            IsConnected = true,
            Last30Days = Enumerable.Range(0, 30)
                .Select(i => new DailyStepPoint { Date = DateTime.UtcNow.Date.AddDays(i - 29), Steps = i * 1000 })
                .ToList()
        };

        Assert.True(analytics.IsConnected);
        Assert.Equal(30, analytics.Last30Days.Count);
        Assert.Equal(29_000, analytics.Last30Days[^1].Steps);
    }
}
