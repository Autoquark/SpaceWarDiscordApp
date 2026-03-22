using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic;

public static class GameEventDispatcher
{
    internal static readonly GameEventDispatcher<Game> Instance =
        new(GameFlowOperations.PlayerChoiceEventResolvedAsync);

    public static void RegisterHandler(object handler) => Instance.RegisterHandler(handler);

    public static Task<DiscordMultiMessageBuilder> ShowPlayerChoicesForEvent(DiscordMultiMessageBuilder builder,
        GameEvent_PlayerChoice gameEvent, Game game, IServiceProvider serviceProvider)
        => Instance.ShowPlayerChoicesForEvent(builder, gameEvent, game, serviceProvider);

    public static Task<DiscordMultiMessageBuilder?> HandlePlayerChoiceInteractionAsync<TEvent, TInteractionData>(
        DiscordMultiMessageBuilder? builder, TEvent gameEvent, TInteractionData choice, Game game, IServiceProvider serviceProvider)
        where TInteractionData : InteractionData
        where TEvent : GameEvent_PlayerChoice<TInteractionData>
        => Instance.HandlePlayerChoiceInteractionAsync(builder, gameEvent, choice, game, serviceProvider);

    public static Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent gameEvent, Game game,
        IServiceProvider serviceProvider)
        => Instance.HandleEventResolvedAsync(builder, gameEvent, game, serviceProvider);
}
