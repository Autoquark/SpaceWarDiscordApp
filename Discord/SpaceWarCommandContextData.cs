using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.Discord;

/// <summary>
/// Allows access to contextual data relevant to a particular execution of a command
/// </summary>
public class SpaceWarCommandContextData
{
    public Game? Game { get; set; }
}