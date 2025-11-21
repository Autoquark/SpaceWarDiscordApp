using DSharpPlus.Entities;

namespace SpaceWarDiscordApp.Discord;

public class SpaceWarInteractionOutcome
{
    public SpaceWarInteractionOutcome(bool requiresSave)
    {
        RequiresSave = requiresSave;
    }

    public bool RequiresSave { get; set; }
    
    public bool DeleteOriginalMessage { get; set; }
}