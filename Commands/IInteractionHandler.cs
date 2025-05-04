using DSharpPlus.EventArgs;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.DatabaseModels;

namespace SpaceWarDiscordApp.Commands;

public interface IInteractionHandler<T> where T : InteractionData
{
    public Task HandleInteractionAsync(T interactionData, Game game, InteractionCreatedEventArgs args);
}