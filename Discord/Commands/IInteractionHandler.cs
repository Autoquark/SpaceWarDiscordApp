using DSharpPlus.EventArgs;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;

namespace SpaceWarDiscordApp.Discord.Commands;

public interface IInteractionHandler<T> where T : InteractionData
{
    public Task<SpaceWarInteractionOutcome> HandleInteractionAsync(T interactionData, Game game, InteractionCreatedEventArgs args);
}