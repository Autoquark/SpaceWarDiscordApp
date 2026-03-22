using System.Reflection;
using Tumult.Database;
using Tumult.Database.GameEvents;
using Tumult.Database.Interactions;
using Tumult.GameLogic;

namespace Tumult.Discord;

public class InteractionDispatcher<TGame> where TGame : BaseGame
{
    private readonly Dictionary<Type, object> _interactionHandlers = new();
    private readonly GameEventDispatcher<TGame> _gameEventDispatcher;

    public InteractionDispatcher(GameEventDispatcher<TGame> gameEventDispatcher)
    {
        _gameEventDispatcher = gameEventDispatcher;
    }

    public void RegisterInteractionHandler(object handler)
    {
        foreach (var interactionType in handler.GetType()
                     .GetInterfaces()
                     .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IInteractionHandler<,>))
                     .Select(x => x.GetGenericArguments()[0]))
        {
            if (!_interactionHandlers.TryAdd(interactionType, handler))
                throw new Exception($"Handler already registered for {interactionType}");
        }
    }

    /// <summary>
    /// Allows game logic to trigger resolution of an interaction directly.
    /// </summary>
    public async Task<InteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        InteractionData interaction,
        TGame game,
        IServiceProvider serviceProvider)
    {
        if (!game.DocumentId!.Equals(interaction.Game))
            throw new ArgumentException("InteractionData does not belong to the given game");

        return await HandleInteractionInternalAsync(builder, interaction, game, serviceProvider);
    }

    /// <summary>
    /// Dispatches an interaction after all authorization/context setup has been done by the caller.
    /// Called by the game's Discord event handler after loading the game and acquiring the lock.
    /// </summary>
    public async Task<InteractionOutcome> HandleInteractionInternalAsync(DiscordMultiMessageBuilder? builder,
        InteractionData interaction,
        TGame game,
        IServiceProvider serviceProvider) =>
        await (Task<InteractionOutcome>)GetType()
            .GetMethod(nameof(HandleTypedInteractionInternalAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(interaction.GetType())
            .Invoke(this, [builder, interaction, game, serviceProvider])!;

    private async Task<InteractionOutcome> HandleTypedInteractionInternalAsync<TInteractionData>(
        DiscordMultiMessageBuilder? builder,
        TInteractionData interaction,
        TGame game,
        IServiceProvider serviceProvider)
        where TInteractionData : InteractionData
    {
        var currentEvent = game.EventStack.LastOrDefault();
        if (interaction.ResolvesChoiceEventId != null)
        {
            if (currentEvent is GameEvent_PlayerChoice<TInteractionData> choiceEvent)
            {
                await (Task<DiscordMultiMessageBuilder?>)typeof(GameEventDispatcher<TGame>)
                    .GetMethod(nameof(GameEventDispatcher<TGame>.HandlePlayerChoiceInteractionAsync))!
                    .MakeGenericMethod(currentEvent.GetType(), typeof(TInteractionData))
                    .Invoke(_gameEventDispatcher, [builder, choiceEvent, interaction, game, serviceProvider])!;
                return new InteractionOutcome(true);
            }

            builder?.AppendContentNewline("These buttons are not for the currently resolving effect.");
            return new InteractionOutcome(false);
        }

        var interactionType = interaction.GetType();
        if (!_interactionHandlers.TryGetValue(interactionType, out var handler))
            throw new Exception("Handler not found");

        if (interaction is EventModifyingInteractionData eventModifyingInteractionData)
        {
            var baseType = interactionType;
            while (baseType != null &&
                   (!baseType.IsGenericType ||
                    baseType.GetGenericTypeDefinition() != typeof(EventModifyingInteractionData<>)))
            {
                baseType = baseType.BaseType;
            }

            if (baseType != null)
            {
                var eventType = baseType.GetGenericArguments()[0];
                if (currentEvent == null || !currentEvent.GetType().IsAssignableTo(eventType) || !eventModifyingInteractionData.EventId.Equals(currentEvent.EventId))
                {
                    builder?.AppendContentNewline("These buttons are not for the currently resolving effect.");
                    return new InteractionOutcome(false);
                }

                interactionType.GetProperty(nameof(EventModifyingInteractionData<GameEvent>.Event))!
                    .SetValue(interaction, currentEvent);
            }
        }

        return await ((IInteractionHandler<TInteractionData, TGame>)handler).HandleInteractionAsync(builder, interaction, game, serviceProvider);
    }
}
