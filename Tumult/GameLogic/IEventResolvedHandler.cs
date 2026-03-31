using Tumult.Database;
using Tumult.Database.GameEvents;
using Tumult.Discord;

namespace Tumult.GameLogic;

/// <summary>
/// Indicates that this type handles continuing the game flow after any triggers resulting from an event of the given type
/// have been resolved
/// </summary>
public interface IEventResolvedHandler<TEvent, TGame>
    where TEvent : GameEvent
    where TGame : BaseGame
{
    Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, TEvent gameEvent,
        TGame game, IServiceProvider serviceProvider);
}
