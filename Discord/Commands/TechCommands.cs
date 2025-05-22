using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.Discord.Commands;

public class TechCommands : IInteractionHandler<UseTechActionInteraction>
{
    public async Task HandleInteractionAsync(UseTechActionInteraction interactionData, Game game, InteractionCreatedEventArgs args)
    {
        var tech = Tech.TechsById[interactionData.TechId];
        var player = game.GetGamePlayerByGameId(interactionData.UsingPlayerId);
        var builder = new DiscordWebhookBuilder();
        
        await tech.UseTechActionAsync(builder, game, player);

        await args.Interaction.EditOriginalResponseAsync(builder);
    }
}