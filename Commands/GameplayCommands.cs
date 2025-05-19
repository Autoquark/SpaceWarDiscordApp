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
        
        var builder = new DiscordMessageBuilder().EnableV2Components();
        await ShowBoardStateMessageAsync(builder, game);
        await context.RespondAsync(builder);
    }

    public static async Task<TBuilder> ShowBoardStateMessageAsync<TBuilder>(TBuilder builder, Game game) 
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        using var image = BoardImageGenerator.GenerateBoardImage(game);
        var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;

        var name = await game.CurrentTurnPlayer.GetNameAsync(false);
        return builder
            .AppendContentNewline(
                $"Board state for {Program.TextInfo.ToTitleCase(game.Name)} at turn {game.TurnNumber} ({name}'s turn)")
            .AddFile("board.png", stream)
        //.AddFileComponent(new DiscordFileComponent("attachment://board.png", false));
        .AddMediaGalleryComponent(new DiscordMediaGalleryItem("attachment://board.png"));
    }
    
    public static async Task<TBuilder> ShowTurnBeginMessageAsync<TBuilder>(TBuilder builder, Game game) 
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var name = await game.CurrentTurnPlayer.GetNameAsync(true);

        List<int> allowedIds = game.CurrentTurnPlayer.IsDummyPlayer ? [] : [game.CurrentTurnPlayer.GamePlayerId];
        var moveInteractionId = await InteractionsHelper.SetUpInteractionAsync(new ShowMoveOptionsInteraction()
        {
            Game = game.DocumentId,
            ForGamePlayerId = game.CurrentTurnPlayer.GamePlayerId,
            AllowedGamePlayerIds = allowedIds, 
        });

        var produceInteractionId = await InteractionsHelper.SetUpInteractionAsync(new ShowProduceOptionsInteraction
        {
            Game = game.DocumentId,
            ForPlayerGameId = game.CurrentTurnPlayer.GamePlayerId,
            AllowedGamePlayerIds = allowedIds,
        });

        var refreshInteractionId = await InteractionsHelper.SetUpInteractionAsync(new RefreshActionInteraction
        {
            Game = game.DocumentId,
            ForPlayerId = game.CurrentTurnPlayer.GamePlayerId,
            AllowedGamePlayerIds = allowedIds
        });
        
        await ShowBoardStateMessageAsync(builder, game);
        builder.AppendContentNewline($"{name}, it is your turn. Choose an action:")
            .AddActionRowComponent(
                new DiscordButtonComponent(DiscordButtonStyle.Primary, moveInteractionId, "Move Action"),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, produceInteractionId, "Produce Action"),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, refreshInteractionId, "Refresh Action")
            );
        return builder;
    }

    public static async Task NextTurnAsync<TBuilder>(TBuilder builder, Game game) where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        game.CurrentTurnPlayerIndex = (game.CurrentTurnPlayerIndex + 1) % game.Players.Count;
        game.TurnNumber++;
        
        await ShowTurnBeginMessageAsync(builder, game);
    }

    [Command("Test")]
    [RequireGuild]
    public static async Task TestCommand(CommandContext context)
    {
        var builder = new DiscordMessageBuilder().EnableV2Components();
        builder.AddTextDisplayComponent("Test Message");
        await using var file = new FileStream("Icons\\dice-six-faces-six.png", FileMode.Open);
        builder.AddFile("test.png", file);
        builder.AddMediaGalleryComponent(new DiscordMediaGalleryItem("attachment://test.png"));
        await context.RespondAsync(builder);

        await Task.Delay(1000);

        var editBuilder = new DiscordMessageBuilder(builder);
        editBuilder.AddSeparatorComponent(new DiscordSeparatorComponent(true, DiscordSeparatorSpacing.Large));
        editBuilder.AddTextDisplayComponent("Is image still there?");
        await context.EditResponseAsync(editBuilder);
    }
}