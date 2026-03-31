using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.Discord.Commands;

public interface ISpaceWarInteractionHandler<T> : IInteractionHandler<T, Game>
    where T : InteractionData
{
}
