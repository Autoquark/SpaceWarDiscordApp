namespace SpaceWarDiscordApp;

public static class RandomExtensions
{
    public static bool NextBool(this Random random, double chance = 0.5f) => random.NextDouble() < chance;
}