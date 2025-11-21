using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Discord;

namespace SpaceWarDiscordApp.GameLogic;

public interface IPlayerChoiceEventHandler<TEvent, TInteractionData>
    where TEvent : GameEvent_PlayerChoice<TInteractionData>
    where TInteractionData : InteractionData
{
    public Task<DiscordMultiMessageBuilder?> ShowPlayerChoicesAsync(DiscordMultiMessageBuilder builder,
        TEvent gameEvent,
        Game game, IServiceProvider serviceProvider);

    /// <summary>
    /// Called when an interaction relating to a player choice is received
    /// </summary>
    /// <returns>Whether the choice event should be considered resolved</returns>
    public Task<bool> HandlePlayerChoiceEventInteractionAsync(DiscordMultiMessageBuilder? builder, TEvent gameEvent,
        TInteractionData choice,
        Game game, IServiceProvider serviceProvider);
}