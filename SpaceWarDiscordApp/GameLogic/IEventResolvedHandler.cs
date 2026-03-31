using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.GameLogic;

public interface IEventResolvedHandler<T> : IEventResolvedHandler<T, Game>
    where T : GameEvent
{
}
