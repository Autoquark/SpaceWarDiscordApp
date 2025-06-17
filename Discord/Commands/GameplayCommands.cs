using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Discord.ContextChecks;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.Discord.Commands;

public class GameplayCommands : IInteractionHandler<EndTurnInteraction>
{
    [Command("ShowBoard")]
    [RequireGameChannel(RequireGameChannelMode.ReadOnly)]
    public static async Task ShowBoardStateCommand(CommandContext context)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();

        if (game.Hexes.Count == 0)
        {
            outcome.RequiresSave = false;
            outcome.SetSimpleReply("No map has yet been generated for this game");
            return;
        }
        
        var builder = new DiscordMessageBuilder().EnableV2Components();
        await GameFlowOperations.ShowBoardStateMessageAsync(builder, game);
        outcome.ReplyBuilder = builder;
    }

    [Command("TurnMessage")]
    [Description("Repost the start of turn message for the current player")]
    [RequireGameChannel(RequireGameChannelMode.ReadOnly)]
    public static async Task TurnMessageCommand(CommandContext context)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();
        
        var builder = new DiscordMessageBuilder().EnableV2Components();
        await GameFlowOperations.ShowSelectActionMessageAsync(builder, game);
        
        outcome.ReplyBuilder = builder;
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(EndTurnInteraction interactionData, Game game, InteractionCreatedEventArgs args)
    {
        var builder = new DiscordWebhookBuilder().EnableV2Components();
        await GameFlowOperations.NextTurnAsync(builder, game);

        return new SpaceWarInteractionOutcome(true, builder);
    }
}