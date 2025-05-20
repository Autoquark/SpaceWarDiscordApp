using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.RegularExpressions;
using CommunityToolkit.HighPerformance.Helpers;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using SixLabors.ImageSharp;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.DatabaseModels;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;
using SpaceWarDiscordApp.ImageGeneration;

namespace SpaceWarDiscordApp.Commands;

public class GameplayCommands
{
    [Command("ShowBoard")]
    [RequireGuild]
    public static async Task ShowBoardStateCommand(CommandContext context)
    {
        await context.DeferResponseAsync();

        var game = await Program.FirestoreDb.RunTransactionAsync(async transaction => await transaction.GetGameForChannelAsync(context.Channel.Id));

        if (game == null)
        {
            await context.RespondAsync("This command must be used from a game channel");
            return;
        }

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
    [RequireGuild]
    public static async Task TurnMessageCommand(CommandContext context)
    {
        await context.DeferResponseAsync();
        
        var game = await Program.FirestoreDb.RunTransactionAsync(async transaction => await transaction.GetGameForChannelAsync(context.Channel.Id));
        if (game == null)
        {
            throw new Exception("This command can only be used in a game channel");
        }
        
        var builder = new DiscordMessageBuilder().EnableV2Components();
        await GameFlowOperations.ShowTurnBeginMessageAsync(builder, game);

        await context.RespondAsync(builder);
    }
}