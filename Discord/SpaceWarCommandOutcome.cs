using DSharpPlus.Entities;

namespace SpaceWarDiscordApp.Discord;

public class SpaceWarCommandOutcome
{
    public SpaceWarCommandOutcome()
    {
    }

    public SpaceWarCommandOutcome(bool requiresSave)
    {
        RequiresSave = requiresSave;
    }

    public bool? RequiresSave { get; set; } = null;
}