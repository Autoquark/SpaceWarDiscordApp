using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.GameLogic;

public static class GameConstants
{
    public const int MaxForcesPerPlanet = 6;
    public const int UniversalTechCount = 3;
    public const int MarketTechCount = 3;
    public const int UniversalTechCost = 2;

    public static readonly TimeSpan TurnProdInterval = TimeSpan.FromDays(1);
    public static readonly TimeSpan UnfinishedTurnProdTime = TimeSpan.FromMinutes(15);

    public static IReadOnlyDictionary<ScoringRule, List<int>> PossibleVictoryThresholds { get; } = new Dictionary<ScoringRule, List<int>>
    {
        { ScoringRule.Cumulative, [50, 100, 150] },
        { ScoringRule.MostStars, [6, 12, 18] }
    };
    
    public static IReadOnlyDictionary<ScoringRule, int> DefaultVictoryThresholds { get; } = new Dictionary<ScoringRule, int>
    {
        { ScoringRule.Cumulative, 100 },
        { ScoringRule.MostStars, 6 }
    };
}