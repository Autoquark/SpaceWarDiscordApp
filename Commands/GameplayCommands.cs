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
        builder
            .AppendContentNewline(
                $"Board state for {Program.TextInfo.ToTitleCase(game.Name)} at turn {game.TurnNumber} ({name}'s turn)")
            .AddFile("board.png", stream)
            .AddMediaGalleryComponent(new DiscordMediaGalleryItem("attachment://board.png"));

        builder.AppendContentNewline("### Player Info");
        
        List<(GamePlayer player, int)>? playerScores = game.Players.Select(x => (x, GetPlayerStars(game, x)))
            .OrderByDescending(x => x.Item2)
            .ToList();

        var playerWillScore = playerScores[0].Item2 > playerScores[1].Item2 ? playerScores[0].player : null;
        
        foreach (var player in game.Players)
        {
            List<string> parts = [await player.GetNameAsync(false), $"Science: {player.Science}", $"VP: {player.VictoryPoints}/6",
                $"Stars: {GetPlayerStars(game, player)}"];
            if (game.CurrentTurnPlayer == player)
            {
                parts.Add("[Current Turn]");
            }

            if (game.ScoringTokenPlayer == player)
            {
                parts.Add("[Scoring Token]");
            }

            if (player == playerWillScore)
            {
                parts.Add("[Most Stars]");
            }
            
            builder.AppendContentNewline(string.Join(" ", parts));
        }

        builder.AppendContentNewline("### Your Turn");
        return builder;
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
        if (game.IsScoringTurn)
        {
            List<(GamePlayer player, int)> playerScores = game.Players.Select(x => (x, GetPlayerStars(game, x)))
                .OrderByDescending(x => x.Item2)
                .ToList();

            if (playerScores[1].Item2 < playerScores[0].Item2)
            {
                var scoringPlayer = playerScores[0].player;
                scoringPlayer.VictoryPoints++;
                var name = await scoringPlayer.GetNameAsync(true);
                builder.AppendContentNewline($"**{name} scores and is now on {game.CurrentTurnPlayer.VictoryPoints}/6 VP!**");
            }

            game.ScoringTokenPlayerIndex--;
            if (game.ScoringTokenPlayerIndex < 0)
            {
                game.ScoringTokenPlayerIndex = game.Players.Count - 1;
            }
            var scoringName = await game.ScoringTokenPlayer.GetNameAsync(false);
            builder.AppendContentNewline($"**The scoring token passes to {scoringName}**");
        }
        
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

    public static int GetPlayerStars(Game game, GamePlayer player)
        => game.Hexes.Where(x => x.Planet?.OwningPlayerId == player.GamePlayerId)
            .Sum(x => x.Planet!.Stars);
}