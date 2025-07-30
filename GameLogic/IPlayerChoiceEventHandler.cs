using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData;

namespace SpaceWarDiscordApp.GameLogic;

public interface IPlayerChoiceEventHandler<TEvent, TInteractionData>
    where TEvent : GameEvent_PlayerChoice<TInteractionData>
    where TInteractionData : InteractionData
{
    public Task<TBuilder?> ShowPlayerChoicesAsync<TBuilder>(TBuilder? builder, TEvent gameEvent,
        Game game, IServiceProvider serviceProvider)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>;

    public Task<TBuilder?> HandlePlayerChoiceEventResolvedAsync<TBuilder>(TBuilder? builder, TEvent gameEvent, TInteractionData choice,
        Game game, IServiceProvider serviceProvider)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>;
}