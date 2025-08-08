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

    public Task<DiscordMultiMessageBuilder?> HandlePlayerChoiceEventResolvedAsync(DiscordMultiMessageBuilder? builder, TEvent gameEvent,
        TInteractionData choice,
        Game game, IServiceProvider serviceProvider);
}