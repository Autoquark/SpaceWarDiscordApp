using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.GameLogic;

/// <summary>
/// Indicates that this type handles continuing the game flow after any triggers resulting from an event of the given type
/// have been resolved
/// </summary>
/// <typeparam name="T">Subtype of GameEvent handled</typeparam>
public interface IEventResolvedHandler<T> where T : GameEvent
{
    public Task<TBuilder?> HandleEventResolvedAsync<TBuilder>(TBuilder? builder, T gameEvent, Game game, IServiceProvider serviceProvider) where TBuilder : BaseDiscordMessageBuilder<TBuilder>;
}