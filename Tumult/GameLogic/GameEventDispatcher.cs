using System.Diagnostics;
using Tumult.Database;
using Tumult.Database.GameEvents;
using Tumult.Database.Interactions;
using Tumult.Discord;

namespace Tumult.GameLogic;

public class GameEventDispatcher<TGame> where TGame : BaseGame
{
    private readonly Dictionary<Type, object> _playerChoiceHandlers = new();
    private readonly Dictionary<Type, object> _eventResolvedHandlers = new();
    private readonly Func<TGame, DiscordMultiMessageBuilder?, IServiceProvider, string, Task> _onPlayerChoiceEventResolved;

    public GameEventDispatcher(Func<TGame, DiscordMultiMessageBuilder?, IServiceProvider, string, Task> onPlayerChoiceEventResolved)
    {
        _onPlayerChoiceEventResolved = onPlayerChoiceEventResolved;
    }

    public void RegisterHandler(object handler)
    {
        foreach (var eventType in handler.GetType()
                     .GetInterfaces()
                     .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEventResolvedHandler<,>))
                     .Select(x => x.GetGenericArguments()[0]))
        {
            if (!_eventResolvedHandlers.TryAdd(eventType, handler))
                throw new Exception($"Handler already registered for {eventType}");
        }

        foreach (var eventType in handler.GetType()
                     .GetInterfaces()
                     .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IPlayerChoiceEventHandler<,,>))
                     .Select(x => x.GetGenericArguments()[0]))
        {
            if (!_playerChoiceHandlers.TryAdd(eventType, handler))
                throw new Exception($"Handler already registered for {eventType}");
        }
    }

    public async Task<DiscordMultiMessageBuilder> ShowPlayerChoicesForEvent(DiscordMultiMessageBuilder builder,
        GameEvent_PlayerChoice gameEvent, TGame game, IServiceProvider serviceProvider)
    {
        var eventType = gameEvent.GetType();
        if (!_playerChoiceHandlers.TryGetValue(eventType, out var handler))
            throw new Exception("Handler not found");

        var genericBase = eventType;
        while (!genericBase.IsGenericType || genericBase.GetGenericTypeDefinition() != typeof(GameEvent_PlayerChoice<>))
        {
            genericBase = genericBase.BaseType!;
        }

        return await (Task<DiscordMultiMessageBuilder>) typeof(IPlayerChoiceEventHandler<,,>)
            .MakeGenericType(eventType, genericBase.GetGenericArguments()[0], typeof(TGame))
            .GetMethod(nameof(IPlayerChoiceEventHandler<GameEvent_PlayerChoice<InteractionData>, InteractionData, TGame>.ShowPlayerChoicesAsync))!
            .Invoke(handler, [builder, gameEvent, game, serviceProvider])!;
    }

    public async Task<DiscordMultiMessageBuilder?> HandlePlayerChoiceInteractionAsync<TEvent, TInteractionData>(
        DiscordMultiMessageBuilder? builder, TEvent gameEvent, TInteractionData choice, TGame game, IServiceProvider serviceProvider)
        where TInteractionData : InteractionData
        where TEvent : GameEvent_PlayerChoice<TInteractionData>
    {
        Debug.Assert(gameEvent.GetType() == typeof(TEvent));
        if (!_playerChoiceHandlers.TryGetValue(typeof(TEvent), out var handler))
            throw new Exception("Handler not found");

        if (await ((IPlayerChoiceEventHandler<TEvent, TInteractionData, TGame>)handler)
            .HandlePlayerChoiceEventInteractionAsync(builder, gameEvent, choice, game, serviceProvider))
        {
            await _onPlayerChoiceEventResolved(game, builder, serviceProvider, gameEvent.EventId);
        }

        return builder;
    }

    public async Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent gameEvent, TGame game,
        IServiceProvider serviceProvider)
    {
        var eventType = gameEvent.GetType();
        if (!_eventResolvedHandlers.TryGetValue(eventType, out var handler))
            throw new Exception("Handler not found");

        return await (Task<DiscordMultiMessageBuilder?>) typeof(IEventResolvedHandler<,>)
            .MakeGenericType(eventType, typeof(TGame))
            .GetMethod(nameof(IEventResolvedHandler<GameEvent, TGame>.HandleEventResolvedAsync))!
            .Invoke(handler, [builder, gameEvent, game, serviceProvider])!;
    }
}
