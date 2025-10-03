using DSharpPlus.Commands.ContextChecks;
using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.Discord.ContextChecks;

public enum RequireGameChannelMode
{
    /// <summary>
    /// Does not require the command to be executed in a game channel.
    /// </summary>
    DoNotRequire,

    /// <summary>
    /// Requires the command to be executed in a game channel but will not modify the game state so does not need it to
    /// be saved afterwards
    /// </summary>
    ReadOnly,
    
    /// <summary>
    /// Requires the command to be executed in a game channel and may modify the game state; game state must be saved
    /// after the command is executed.
    /// </summary>
    RequiresSave
}

/// <summary>
/// Requires the command to be executed in a channel associated with a game. Also causes the game to be retrieved from
/// the DB and made available via SpaceWarCommandContextData
/// 
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireGameChannelAttribute(RequireGameChannelMode mode) : Attribute
{
    public RequireGameChannelAttribute(RequireGameChannelMode mode, GamePhase requiredPhase) : this(mode)
    {
        RequiredPhase = requiredPhase;
    }
    
    public RequireGameChannelMode Mode { get; } = mode;
    
    public GamePhase? RequiredPhase { get; } = null;
}