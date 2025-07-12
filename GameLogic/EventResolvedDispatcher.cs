using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.GameLogic;

public static class EventResolvedDispatcher
{
    private static readonly Dictionary<Type, object> Handlers = new();
    
    public static void RegisterHandler(object interactionHandler)
    {
        foreach (var interactionType in interactionHandler.GetType()
                     .GetInterfaces()
                     .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEventResolvedHandler<>))
                     .Select(x => x.GetGenericArguments()[0]))
        {
            if (!Handlers.TryAdd(interactionType, interactionHandler))
            {
                throw new Exception($"Handler already registered for {interactionType}");
            }
        }
    }
    
    public static async Task<TBuilder> HandleEventResolvedAsync<TBuilder>(TBuilder builder, GameEvent gameEvent, Game game,
        IServiceProvider serviceProvider) where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var eventType = gameEvent.GetType();
        if (!Handlers.TryGetValue(eventType, out var handler))
        {
            throw new Exception("Handler not found");
        }

        return await (Task<TBuilder>) typeof(IEventResolvedHandler<>).MakeGenericType(eventType)
            .GetMethod(nameof(IEventResolvedHandler<GameEvent>.HandleEventResolvedAsync))!
            .MakeGenericMethod(typeof(TBuilder))
            .Invoke(handler, [builder, gameEvent, game, serviceProvider])!;
    }
}