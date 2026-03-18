using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;

namespace SpaceWarDiscordApp.Discord.Commands;

public interface IInteractionHandler<T> where T : InteractionData
{
    public Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        T interactionData,
        Game game,
        IServiceProvider serviceProvider);
}