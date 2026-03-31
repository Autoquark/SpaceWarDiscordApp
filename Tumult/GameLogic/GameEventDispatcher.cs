using System.Diagnostics;
using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;
using Tumult.Database;
using Tumult.Database.GameEvents;
using Tumult.Database.Interactions;
using Tumult.Discord;

namespace Tumult.GameLogic;

public abstract class GameEventDispatcher<TGame> where TGame : BaseGame
{
    private readonly Dictionary<Type, object> _playerChoiceHandlers = new();
    private readonly Dictionary<Type, object> _eventResolvedHandlers = new();
    protected readonly FirestoreDb FirestoreDb;

    protected GameEventDispatcher(FirestoreDb firestoreDb)
    {
        FirestoreDb = firestoreDb;
    }

    /// <summary>
    /// Returns the triggered effects that should be presented to the given player for the given event.
    /// </summary>
    protected abstract IEnumerable<TriggeredEffect> GetTriggeredEffectsForPlayer(TGame game, GameEvent gameEvent, BaseGamePlayer player);

    /// <summary>
    /// Returns the IDs of players that should be offered triggered effects for newly pushed events, in resolution order.
    /// </summary>
    protected abstract IEnumerable<int> GetPlayerIdsToResolveTriggersFor(TGame game);

    /// <summary>
    /// Called after the event stack loop finishes (whether the stack is empty or paused waiting for player input).
    /// Use to advance game state when the stack is empty.
    /// </summary>
    protected abstract Task<DiscordMultiMessageBuilder?> OnEventStackEmptyAsync(DiscordMultiMessageBuilder? builder, TGame game, IServiceProvider serviceProvider);

    /// <summary>
    /// Called when multiple triggered effects are pending for a player and a choice is required.
    /// Responsible for showing the player their options. The loop will break after this returns.
    /// </summary>
    protected abstract Task ShowTriggeredEffectsChoiceAsync(DiscordMultiMessageBuilder? builder, GameEvent resolvingEvent, TGame game, IServiceProvider serviceProvider);

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

    /// <summary>
    /// Pushes a sequence of game events onto the stack and continues resolving. Events resolve in the order supplied.
    /// </summary>
    public async Task<DiscordMultiMessageBuilder?> PushGameEventsAndResolveAsync(DiscordMultiMessageBuilder? builder, TGame game,
        IServiceProvider serviceProvider, params IEnumerable<GameEvent> gameEvents)
    {
        PushGameEvents(game, gameEvents);
        return await ContinueResolvingEventStackAsync(builder, game, serviceProvider);
    }

    /// <summary>
    /// Pushes a sequence of game events onto the stack. Events resolve in the order supplied.
    /// </summary>
    public void PushGameEvents(TGame game, params IEnumerable<GameEvent> gameEvents)
    {
        foreach (var gameEvent in gameEvents.Reverse())
        {
            gameEvent.PlayerIdsToResolveTriggersFor = GetPlayerIdsToResolveTriggersFor(game).ToList();
            game.EventStack.Add(gameEvent);
        }
    }

    public async Task<DiscordMultiMessageBuilder?> PlayerChoiceEventResolvedAsync(TGame game,
        DiscordMultiMessageBuilder? builder, IServiceProvider serviceProvider, string eventId)
    {
        var relevantEvent = game.EventStack.FirstOrDefault(x => x.EventId == eventId);
        if (relevantEvent is not GameEvent_PlayerChoice)
        {
            throw new Exception("Event not found or is not a player choice event");
        }

        game.EventStack.Remove(relevantEvent);
        return await ContinueResolvingEventStackAsync(builder, game, serviceProvider);
    }

    public async Task<DiscordMultiMessageBuilder?> TriggerResolvedAsync(TGame game, DiscordMultiMessageBuilder? builder,
        IServiceProvider serviceProvider, string interactionId)
    {
        // The interaction might not be for the top event on the stack if, in the process of resolving it, we pushed events onto the stack
        var relevantEvent = game.EventStack.FirstOrDefault(x =>
            x.RemainingTriggersToResolve.Any(y => y.ResolveInteractionId == interactionId));

        var triggeredEffect = relevantEvent?.RemainingTriggersToResolve.Find(x => x.ResolveInteractionId == interactionId);
        if (triggeredEffect == null)
        {
            throw new Exception("Triggered effect not found");
        }

        relevantEvent!.TriggerIdsResolved.Add(triggeredEffect.TriggerId);

        // Reevaluate triggers for the current player; this trigger may have changed game state and caused
        // triggers to become available or unavailable. TriggerIdsResolved prevents re-resolving the same trigger.
        var resolvingPlayer = game.GamePlayers.First(x => x.GamePlayerId == relevantEvent.ResolvingTriggersForPlayerId);
        relevantEvent.RemainingTriggersToResolve = GetTriggeredEffectsForPlayer(game, relevantEvent, resolvingPlayer).ToList();

        serviceProvider.AddInteractionsToSetUp(relevantEvent.RemainingTriggersToResolve
            .Select(x => x.ResolveInteractionData).Where(x => x != null).Select(x => x!));

        return await ContinueResolvingEventStackAsync(builder, game, serviceProvider);
    }

    public async Task<DiscordMultiMessageBuilder?> DeclineOptionalTriggersAsync(DiscordMultiMessageBuilder? builder,
        TGame game, IServiceProvider serviceProvider)
    {
        var gameEvent = game.EventStack.LastOrDefault();
        if (gameEvent == null)
        {
            return builder;
        }

        if (gameEvent.RemainingTriggersToResolve.Any(x => x.IsMandatory))
        {
            Debug.Assert(false);
            return builder;
        }

        gameEvent.RemainingTriggersToResolve.Clear();

        return await ContinueResolvingEventStackAsync(builder, game, serviceProvider);
    }

    public async Task<DiscordMultiMessageBuilder?> ContinueResolvingEventStackAsync(DiscordMultiMessageBuilder? builder, TGame game, IServiceProvider serviceProvider)
    {
        var resolveState = serviceProvider.GetRequiredService<PerOperationState>();
        if (resolveState.IsResolvingStack)
        {
            return builder;
        }

        resolveState.IsResolvingStack = true;

        while (game.EventStack.Count > 0)
        {
            var resolvingEvent = game.EventStack.Last();

            var autoResolveTrigger = resolvingEvent.RemainingTriggersToResolve.FirstOrDefault(x => x.AlwaysAutoResolve);
            if (autoResolveTrigger != null)
            {
                await ResolveTriggeredEffectAsync(builder, game, autoResolveTrigger, serviceProvider);
                continue;
            }

            // If there is only one trigger and it's mandatory, we can auto resolve it
            if (resolvingEvent.RemainingTriggersToResolve is [{ IsMandatory: true }])
            {
                var resolvingTrigger = resolvingEvent.RemainingTriggersToResolve[0];
                await ResolveTriggeredEffectAsync(builder, game, resolvingTrigger, serviceProvider);
            }
            // Multiple and/or optional triggers: let the game present a player decision
            else if (resolvingEvent.RemainingTriggersToResolve.Count > 0)
            {
                await ShowTriggeredEffectsChoiceAsync(builder, resolvingEvent, game, serviceProvider);
                break;
            }
            else
            {
                // Move on to the next player's triggers
                if (resolvingEvent.PlayerIdsToResolveTriggersFor.Count > 0)
                {
                    var player = game.GamePlayers.First(x => x.GamePlayerId == resolvingEvent.PlayerIdsToResolveTriggersFor[0]);
                    resolvingEvent.PlayerIdsToResolveTriggersFor.RemoveAt(0);
                    resolvingEvent.ResolvingTriggersForPlayerId = player.GamePlayerId;
                    resolvingEvent.RemainingTriggersToResolve = GetTriggeredEffectsForPlayer(game, resolvingEvent, player).ToList();

                    serviceProvider.AddInteractionsToSetUp(resolvingEvent.RemainingTriggersToResolve
                        .Select(x => x.ResolveInteractionData).Where(x => x != null).Select(x => x!));

                    continue;
                }

                // Player choice event: display choices and pause resolving
                if (resolvingEvent is GameEvent_PlayerChoice choiceEvent)
                {
                    if (builder != null)
                    {
                        await ShowPlayerChoicesForEvent(builder, choiceEvent, game, serviceProvider);

                        // If showing the choices caused the stack to change, continue resolving
                        if (game.EventStack.Count == 0 || game.EventStack[^1] != resolvingEvent)
                        {
                            continue;
                        }
                    }
                }
                // No more players to resolve: pop this event and invoke its OnResolve handler
                else
                {
                    await PopEventFromStackAndResolveAsync(builder, game, serviceProvider);
                    continue;
                }

                // Event requires explicit player resolve; pause resolution
                break;
            }
        }

        resolveState.IsResolvingStack = false;

        return await OnEventStackEmptyAsync(builder, game, serviceProvider);
    }

    private async Task PopEventFromStackAndResolveAsync(DiscordMultiMessageBuilder? builder, TGame game,
        IServiceProvider serviceProvider)
    {
        var resolving = game.EventStack.LastOrDefault();
        if (resolving == null)
        {
            throw new Exception("No events to resolve");
        }

        game.EventStack.RemoveAt(game.EventStack.Count - 1);
        await HandleEventResolvedAsync(builder, resolving, game, serviceProvider);
    }

    private async Task ResolveTriggeredEffectAsync(DiscordMultiMessageBuilder? builder, TGame game,
        TriggeredEffect triggeredEffect, IServiceProvider serviceProvider)
    {
        var interactionData = triggeredEffect.ResolveInteractionData
            ?? await LoadTriggeredEffectInteractionDataAsync(triggeredEffect.ResolveInteractionId);

        if (interactionData == null)
        {
            throw new Exception("Interaction data not found");
        }

        await serviceProvider.GetRequiredService<InteractionDispatcher<TGame>>()
            .HandleInteractionAsync(builder, interactionData, game, serviceProvider);
    }

    private async Task<TriggeredEffectInteractionData?> LoadTriggeredEffectInteractionDataAsync(string interactionId)
    {
        var snapshot = await FirestoreDb.InteractionData()
            .WhereEqualTo(nameof(InteractionData.InteractionId), interactionId)
            .Limit(1)
            .GetSnapshotAsync();
        var doc = snapshot.Documents.FirstOrDefault();
        if (doc == null)
        {
            return null;
        }
        return doc.ConvertTo<InteractionData>() as TriggeredEffectInteractionData;
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

        return await (Task<DiscordMultiMessageBuilder>)typeof(IPlayerChoiceEventHandler<,,>)
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
            await PlayerChoiceEventResolvedAsync(game, builder, serviceProvider, gameEvent.EventId);
        }

        return builder;
    }

    public async Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent gameEvent, TGame game,
        IServiceProvider serviceProvider)
    {
        var eventType = gameEvent.GetType();
        if (!_eventResolvedHandlers.TryGetValue(eventType, out var handler))
            throw new Exception("Handler not found");

        return await (Task<DiscordMultiMessageBuilder?>)typeof(IEventResolvedHandler<,>)
            .MakeGenericType(eventType, typeof(TGame))
            .GetMethod(nameof(IEventResolvedHandler<GameEvent, TGame>.HandleEventResolvedAsync))!
            .Invoke(handler, [builder, gameEvent, game, serviceProvider])!;
    }
}
