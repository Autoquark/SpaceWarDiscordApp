using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.Discord.Commands;

public interface IInteractionHandler<T> : IInteractionHandler<T, Game>
    where T : InteractionData
{
}
