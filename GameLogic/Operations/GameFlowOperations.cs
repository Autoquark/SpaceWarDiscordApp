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

        builder.AppendContentNewline("Player Info".DiscordHeading3());
        
        List<(GamePlayer player, int)>? playerScores = game.Players.Where(x => !x.IsEliminated)
            .Select(x => (x, GameStateOperations.GetPlayerStars(game, x)))
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

            var text = string.Join(" ", parts);

            if (player.IsEliminated)
            {
                text = text.DiscordStrikeThrough();
            }
            else if (player == game.CurrentTurnPlayer)
            {
                text = text.DiscordBold();
            }
            
            builder.AppendContentNewline(text);
        }

        builder.AppendContentNewline("Your Turn".DiscordHeading3());
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
    public static async Task NextTurnAsync<TBuilder>(TBuilder? builder, Game game) where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        if (game.IsScoringTurn)
        {
            List<(GamePlayer player, int)> playerScores = game.Players.Where(x => !x.IsEliminated)
                .Select(x => (x, GameStateOperations.GetPlayerStars(game, x)))
                .OrderByDescending(x => x.Item2)
                .ToList();

            if (playerScores[1].Item2 < playerScores[0].Item2)
            {
                var scoringPlayer = playerScores[0].player;
                scoringPlayer.VictoryPoints++;
                var name = await scoringPlayer.GetNameAsync(true);
                builder?.AppendContentNewline($"**{name} scores and is now on {scoringPlayer.VictoryPoints}/6 VP!**");

                if (scoringPlayer.VictoryPoints >= 6)
                {
                    builder?.AppendContentNewline($"{name} has won the game!".DiscordHeading1());
                    builder?.AppendContentNewline("If you want to continue, fix up the game state so there is no longer a winner and use /turnmessage to continue playing");
                    game.Phase = GamePhase.Finished;
                }
            }

            // If someone appears to have won, still finish the end of turn logic (in case the game is fixed up and continued)
            // but don't post any messages about it.
            await CycleScoringTokenAsync(game.Phase == GamePhase.Finished ? null : builder, game);
        }

        do
        {
            game.CurrentTurnPlayerIndex = (game.CurrentTurnPlayerIndex + 1) % game.Players.Count;
        }
        while (game.CurrentTurnPlayer.IsEliminated);
        
        game.TurnNumber++;
        
        if (game.Phase == GamePhase.Finished || builder == null)
        {
            return;
        }
        
        await ShowTurnBeginMessageAsync(builder, game);
    }

    /// <summary>
    /// Check if any players have been eliminated. Note that this can end the game.
    /// </summary>
    public static async Task CheckForPlayerEliminationsAsync<TBuilder>(TBuilder builder, Game game)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        foreach (var player in game.Players.Where(x => !x.IsEliminated))
        {
            if (game.Hexes.Any(x => x.Planet?.OwningPlayerId == player.GamePlayerId && x.Planet.ForcesPresent > 0))
            {
                continue;
            }

            player.IsEliminated = true;
            
            var name = await player.GetNameAsync(true);
            builder.AppendContentNewline($"{name} has been eliminated!".DiscordBold());

            if (game.ScoringTokenPlayer == player)
            {
                await CycleScoringTokenAsync(builder, game);
            }
        }

        var remainingPlayers = game.Players.Count(x => !x.IsEliminated); 
        if (remainingPlayers == 1)
        {
            var name = await game.Players.First(x => !x.IsEliminated).GetNameAsync(true);
            builder.AppendContentNewline($"{name} wins the game through glorious violence by being the last one standing!".DiscordBold());
            game.Phase = GamePhase.Finished;
        }
        else if (remainingPlayers == 0)
        {
            builder.AppendContentNewline("It would appear that you have all wiped each other out, leaving the universe cold and lifeless. Oops.".DiscordBold());
            game.Phase = GamePhase.Finished;
        }
    }

    /// <summary>
    /// Passes the scoring token to the previous valid player in turn order
    /// </summary>
    private static async Task CycleScoringTokenAsync<TBuilder>(TBuilder? builder, Game game)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        do
        {
            game.ScoringTokenPlayerIndex--;
            if (game.ScoringTokenPlayerIndex < 0)
            {
                game.ScoringTokenPlayerIndex = game.Players.Count - 1;
            }
        }
        while (game.ScoringTokenPlayer.IsEliminated);
        
        var scoringName = await game.ScoringTokenPlayer.GetNameAsync(false);
        builder?.AppendContentNewline($"**The scoring token passes to {scoringName}**");
    }
}