using DSharpPlus.Commands.ContextChecks;

namespace SpaceWarDiscordApp.Discord.ContextChecks;

/// <summary>
/// Requires the command to be executed in a channel associated with a game. Also causes the game to be retrieved from
/// the DB and made available via SpaceWarCommandContextData
/// 
/// </summary>
public class RequireGameChannelAttribute : ContextCheckAttribute
{
    
}