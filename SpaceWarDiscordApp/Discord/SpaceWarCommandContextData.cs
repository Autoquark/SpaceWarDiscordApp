using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Discord;

/// <summary>
/// Allows access to contextual data relevant to a particular execution of a command
/// </summary>
public class SpaceWarCommandContextData
{
    public Game? Game { get; set; }
    
    public NonDbGameState? NonDbGameState { get; set; }
    
    public required GlobalData GlobalData { get; set; }
    
    public required DiscordUser User { get; set; }
    
    public required DiscordMessage? InteractionMessage { get; set; }
}