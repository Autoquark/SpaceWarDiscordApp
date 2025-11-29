using System.Diagnostics;
using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic;

public static class GameEventDispatcher
{
    // Maps Event Type (subclass of GameEvent_PlayerChoice) to handler
    // We don't store interaction data type as there should only be one interaction data type for a given event type
    private static readonly Dictionary<Type, object> PlayerChoiceHandlers = new();
    
    private static readonly Dictionary<Type, object> EventResolvedHandlers = new();
    
    public static void RegisterHandler(object interactionHandler)
    {
        foreach (var interactionType in interactionHandler.GetType()
                     .GetInterfaces()
                     .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEventResolvedHandler<>))
                     .Select(x => x.GetGenericArguments()[0]))
        {
            if (!EventResolvedHandlers.TryAdd(interactionType, interactionHandler))
            {
                throw new Exception($"Handler already registered for {interactionType}");
            }
        }
        
        foreach (var interactionType in interactionHandler.GetType()
                     .GetInterfaces()
                     .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IPlayerChoiceEventHandler<,>))
                     .Select(x => x.GetGenericArguments()[0]))
        {
            if (!PlayerChoiceHandlers.TryAdd(interactionType, interactionHandler))
            {
                throw new Exception($"Handler already registered for {interactionType}");
            }
        }
    }

    public static async Task<DiscordMultiMessageBuilder> ShowPlayerChoicesForEvent(DiscordMultiMessageBuilder builder,
        GameEvent_PlayerChoice gameEvent, Game game, IServiceProvider serviceProvider)
    {
        var eventType = gameEvent.GetType();
        if (!PlayerChoiceHandlers.TryGetValue(eventType, out var handler))
        {
            throw new Exception("Handler not found");
        }

        // Go up the inheritance tree until we find GameEvent_PlayerChoice<> as we need to get the interaction data type
        var genericBase = eventType;
        while (!genericBase.IsGenericType || genericBase.GetGenericTypeDefinition() != typeof(GameEvent_PlayerChoice<>))
        {
            genericBase = genericBase.BaseType!;
        }
        
        return await (Task<DiscordMultiMessageBuilder>) typeof(IPlayerChoiceEventHandler<,>).MakeGenericType(eventType, genericBase.GetGenericArguments()[0])
            // Types here are just to make nameof compile
            .GetMethod(nameof(IPlayerChoiceEventHandler<GameEvent_PlayerChoice<InteractionData>, InteractionData>.ShowPlayerChoicesAsync))!
            .Invoke(handler, [builder, gameEvent, game, serviceProvider])!;
    }

    public static async Task<DiscordMultiMessageBuilder?> HandlePlayerChoiceInteractionAsync<TEvent, TInteractionData>(
        DiscordMultiMessageBuilder? builder, TEvent gameEvent, TInteractionData choice, Game game, IServiceProvider serviceProvider)
        where TInteractionData : InteractionData
        where TEvent : GameEvent_PlayerChoice<TInteractionData>
    {
        Debug.Assert(gameEvent.GetType() == typeof(TEvent));
        if (!PlayerChoiceHandlers.TryGetValue(typeof(TEvent), out var handler))
        {
            throw new Exception("Handler not found");
        }
        
        var choiceEvent = (TEvent) game.EventStack[^1];
        if (await ((IPlayerChoiceEventHandler<TEvent, TInteractionData>)handler)
            .HandlePlayerChoiceEventInteractionAsync(builder, gameEvent, choice, game, serviceProvider))
        {
            game.EventStack.Remove(choiceEvent);
            await GameFlowOperations.ContinueResolvingEventStackAsync(builder, game, serviceProvider);
        }

        return builder;
    }

    public static async Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent gameEvent, Game game,
        IServiceProvider serviceProvider)
    {
        var eventType = gameEvent.GetType();
        if (!EventResolvedHandlers.TryGetValue(eventType, out var handler))
        {
            throw new Exception("Handler not found");
        }

        return await (Task<DiscordMultiMessageBuilder>) typeof(IEventResolvedHandler<>).MakeGenericType(eventType)
            // GameEvent here is just to make nameof compile
            .GetMethod(nameof(IEventResolvedHandler<GameEvent>.HandleEventResolvedAsync))!
            .Invoke(handler, [builder, gameEvent, game, serviceProvider])!;
    }
}