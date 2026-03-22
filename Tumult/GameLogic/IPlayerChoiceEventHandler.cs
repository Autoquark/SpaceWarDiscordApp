using Tumult.Database;
using Tumult.Database.GameEvents;
using Tumult.Database.Interactions;
using Tumult.Discord;

namespace Tumult.GameLogic;

public interface IPlayerChoiceEventHandler<TEvent, TInteractionData, TGame>
    where TEvent : GameEvent_PlayerChoice<TInteractionData>
    where TInteractionData : InteractionData
    where TGame : BaseGame
{
    Task<DiscordMultiMessageBuilder?> ShowPlayerChoicesAsync(DiscordMultiMessageBuilder builder,
        TEvent gameEvent,
        TGame game, IServiceProvider serviceProvider);

    /// <summary>
    /// Called when an interaction relating to a player choice is received.
    /// </summary>
    /// <returns>Whether the choice event should be considered resolved</returns>
    Task<bool> HandlePlayerChoiceEventInteractionAsync(DiscordMultiMessageBuilder? builder, TEvent gameEvent,
        TInteractionData choice,
        TGame game, IServiceProvider serviceProvider);
}
