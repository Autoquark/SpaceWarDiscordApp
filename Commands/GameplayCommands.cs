using System.Collections.Specialized;
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
using SpaceWarDiscordApp.ImageGeneration;

namespace SpaceWarDiscordApp.Commands;

public class GameplayCommands
{
    [Command("ShowBoard")]
    [RequireGuild]
    public static async Task ShowBoardState(CommandContext context)
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
        
        await context.RespondAsync(await CreateBoardStateMessageAsync(game));
    }

    public static async Task<DiscordMessageBuilder> CreateBoardStateMessageAsync(Game game)
    {
        using var image = BoardImageGenerator.GenerateBoardImage(game);
        var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;

        var name = await game.CurrentTurnPlayer.GetNameAsync(false);
        return new DiscordMessageBuilder()
            .WithContent(
                $"Board state for {Program.TextInfo.ToTitleCase(game.Name)} at turn {game.TurnNumber} ({name}'s turn)")
            .AddFile("board.png", stream);
    }
    
    public static async Task<IList<DiscordMessageBuilder>> CreateTurnBeginMessagesAsync(Game game)
    {
        var name = await game.CurrentTurnPlayer.GetNameAsync(true);
        
        var interactionId = await InteractionsHelper.SetUpInteractionAsync(new ShowMoveOptionsInteraction()
        {
            Game = game.DocumentId,
            ForGamePlayerId = game.CurrentTurnPlayer.GamePlayerId,
            AllowedGamePlayerIds = game.CurrentTurnPlayer.IsDummyPlayer ? [] : [game.CurrentTurnPlayer.GamePlayerId]
        });
        
        var builder = new DiscordMessageBuilder()
            .WithContent($"{name}, it is your turn. Choose an action:")
            .AddActionRowComponent(
                new DiscordButtonComponent(DiscordButtonStyle.Primary, interactionId, "Move Action")
                //new DiscordButtonComponent(DiscordButtonStyle.Primary, CreateInteractionId("BeginProduceAction", game.CurrentTurnPlayer.DiscordUserId), "Produce Action"),
                //new DiscordButtonComponent(DiscordButtonStyle.Primary, CreateInteractionId("RefreshAction", game.CurrentTurnPlayer.DiscordUserId), "Refresh Action")
            );
        return [await CreateBoardStateMessageAsync(game), builder];
    }

    
}