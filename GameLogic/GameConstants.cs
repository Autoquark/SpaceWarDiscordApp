namespace SpaceWarDiscordApp.GameLogic;

public static class GameConstants
{
    public const int MaxForcesPerPlanet = 6;
    public const int UniversalTechCount = 3;
    public const int MarketTechCount = 3;
    public const int UniversalTechCost = 2;
    public const int VpToWin = 6;

    public static TimeSpan TurnProdInterval = TimeSpan.FromDays(1);
    public static TimeSpan UnfinishedTurnProdTime = TimeSpan.FromMinutes(15);
}