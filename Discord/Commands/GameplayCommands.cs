using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Discord.ContextChecks;
using SpaceWarDiscordApp.GameLogic.Operations;
using TransactionExtensions = SpaceWarDiscordApp.Database.TransactionExtensions;

namespace SpaceWarDiscordApp.Discord.Commands;

[RequireGameChannel]
public class GameplayCommands : IInteractionHandler<EndTurnInteraction>
{
    [Command("ShowBoard")]
    public static async Task ShowBoardStateCommand(CommandContext context)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;

        if (game.Hexes.Count == 0)
        {
            await context.RespondAsync("No map has yet been generated for this game");
            return;
        }
        
        var builder = new DiscordMessageBuilder().EnableV2Components();
        await GameFlowOperations.ShowBoardStateMessageAsync(builder, game);
        await context.RespondAsync(builder);
    }

    [Command("TurnMessage")]
    [Description("Repost the start of turn message for the current player")]
    public static async Task TurnMessageCommand(CommandContext context)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        
        var builder = new DiscordMessageBuilder().EnableV2Components();
        await GameFlowOperations.ShowTurnBeginMessageAsync(builder, game);

        await context.RespondAsync(builder);
    }

    public async Task HandleInteractionAsync(EndTurnInteraction interactionData, Game game, InteractionCreatedEventArgs args)
    {
        var builder = new DiscordWebhookBuilder().EnableV2Components();
        await GameFlowOperations.NextTurnAsync(builder, game);
        await args.Interaction.EditOriginalResponseAsync(builder);
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));
    }
}