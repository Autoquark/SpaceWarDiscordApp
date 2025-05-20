using DSharpPlus.Entities;
using SixLabors.ImageSharp;
using SpaceWarDiscordApp.Commands;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.DatabaseModels;
using SpaceWarDiscordApp.ImageGeneration;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public static class GameFlowOperations
{
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
        
        List<(GamePlayer player, int)>? playerScores = game.Players.Select(x => (x, GameStateOperations.GetPlayerStars(game, x)))
            .OrderByDescending(x => x.Item2)
            .ToList();

        var playerWillScore = playerScores[0].Item2 > playerScores[1].Item2 ? playerScores[0].player : null;
        
        foreach (var player in game.Players)
        {
            List<string> parts = [await player.GetNameAsync(false), $"Science: {player.Science}", $"VP: {player.VictoryPoints}/6",
                $"Stars: {GameStateOperations.GetPlayerStars(game, player)}"];
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
        
        await GameFlowOperations.ShowBoardStateMessageAsync(builder, game);
        builder.AppendContentNewline($"{name}, it is your turn. Choose an action:")
            .AddActionRowComponent(
                new DiscordButtonComponent(DiscordButtonStyle.Primary, moveInteractionId, "Move Action"),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, produceInteractionId, "Produce Action"),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, refreshInteractionId, "Refresh Action")
            );
        return builder;
    }

    /// <summary>
    /// Advances the game to the next turn
    /// </summary>
    public static async Task NextTurnAsync<TBuilder>(TBuilder builder, Game game) where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        if (game.IsScoringTurn)
        {
            List<(GamePlayer player, int)> playerScores = game.Players.Select(x => (x, GameStateOperations.GetPlayerStars(game, x)))
                .OrderByDescending(x => x.Item2)
                .ToList();

            if (playerScores[1].Item2 < playerScores[0].Item2)
            {
                var scoringPlayer = playerScores[0].player;
                scoringPlayer.VictoryPoints++;
                var name = await scoringPlayer.GetNameAsync(true);
                builder.AppendContentNewline($"**{name} scores and is now on {scoringPlayer.VictoryPoints}/6 VP!**");

                if (scoringPlayer.VictoryPoints >= 6)
                {
                    builder.AppendContentNewline($"# {name} has won the game!");
                    builder.AppendContentNewline($"If you want to continue, fix up the game state so there is no longer a winner and use /turnmessage to continue playing");
                    game.Phase = GamePhase.Finished;
                }
            }

            game.ScoringTokenPlayerIndex--;
            if (game.ScoringTokenPlayerIndex < 0)
            {
                game.ScoringTokenPlayerIndex = game.Players.Count - 1;
            }

            // If someone appears to have won, still finish the end of turn logic (in case the game is fixed up and continued)
            // but don't post any messages about it.
            if (game.Phase == GamePhase.Finished)
            {
                return;
            }
            
            var scoringName = await game.ScoringTokenPlayer.GetNameAsync(false);
            builder.AppendContentNewline($"**The scoring token passes to {scoringName}**");
        }
        
        game.CurrentTurnPlayerIndex = (game.CurrentTurnPlayerIndex + 1) % game.Players.Count;
        game.TurnNumber++;
        
        await ShowTurnBeginMessageAsync(builder, game);
    }
}