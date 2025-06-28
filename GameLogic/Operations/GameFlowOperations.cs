using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.Move;
using SpaceWarDiscordApp.Database.InteractionData.Tech;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.GameEvents;
using SpaceWarDiscordApp.GameLogic.Techs;
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

        builder.AppendContentNewline($"Universal Tech ({GameConstants.UniversalTechCost})".DiscordHeading2());
        builder.AppendContentNewline(string.Join(", ", game.UniversalTechs.Select(x => Tech.TechsById[x].DisplayName)));
        
        builder.AppendContentNewline("Tech Market".DiscordHeading2());
        builder.AppendContentNewline(string.Join(", ", game.TechMarket.Select((x, i) => (x == null ? "[empty]" : Tech.TechsById[x].DisplayName) + $" ({TechOperations.GetMarketSlotCost(i)})")));
        
        builder.AppendContentNewline("Player Info".DiscordHeading2());
        
        List<(GamePlayer player, int)> playerScores = game.Players.Where(x => !x.IsEliminated)
            .Select(x => (x, GameStateOperations.GetPlayerStars(game, x)))
            .OrderByDescending(x => x.Item2)
            .ToList();

        var playerWillScore = playerScores[0].Item2 > playerScores[1].Item2 ? playerScores[0].player : null;
        
        foreach (var player in game.Players)
        {
            List<string> parts = [await player.GetNameAsync(false),
                $"Science: {player.Science}", $"VP: {player.VictoryPoints}/6",
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

            var text = new StringBuilder(string.Join(" | ", parts));

            if (player.IsEliminated)
            {
                text = text.DiscordStrikeThrough();
            }
            else if (player == game.CurrentTurnPlayer)
            {
                text = text.DiscordBold();
            }
            
            if (player.Techs.Any())
            {
                text.AppendLine();
                text.AppendJoin(", ", player.Techs.Select(x => Tech.TechsById[x.TechId].GetTechDisplayString(game, player)));
            }
            
            builder.AppendContentNewline(text.ToString());
        }
        
        return builder;
    }

    public static async Task<TBuilder> ShowSelectActionMessageAsync<TBuilder>(TBuilder builder, Game game, IServiceProvider serviceProvider) 
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        if (game.HavePrintedSelectActionThisInteraction)
        {
            return builder;
        }
        
        game.HavePrintedSelectActionThisInteraction = true;
        
        var name = await game.CurrentTurnPlayer.GetNameAsync(true);

        var interactionGroupId = serviceProvider.GetRequiredService<SpaceWarCommandContextData>().GlobalData
            .InteractionGroupId;
        
        var moveInteractionId = await InteractionsHelper.SetUpInteractionAsync(new BeginPlanningMoveInteraction<MoveActionCommands>()
        {
            Game = game.DocumentId,
            ForGamePlayerId = game.CurrentTurnPlayer.GamePlayerId,
        }, interactionGroupId);

        var produceInteractionId = await InteractionsHelper.SetUpInteractionAsync(new ShowProduceOptionsInteraction
        {
            Game = game.DocumentId,
            ForGamePlayerId = game.CurrentTurnPlayer.GamePlayerId,
        }, interactionGroupId);

        var refreshInteractionId = await InteractionsHelper.SetUpInteractionAsync(new RefreshActionInteraction
        {
            Game = game.DocumentId,
            ForGamePlayerId = game.CurrentTurnPlayer.GamePlayerId,
        }, interactionGroupId);;

        var endTurnInteractionId = await InteractionsHelper.SetUpInteractionAsync(new EndTurnInteraction
        {
            ForGamePlayerId = game.CurrentTurnPlayer.GamePlayerId,
            Game = game.DocumentId,
            EditOriginalMessage = false
        }, interactionGroupId);;

        var techActions = GetPlayerTechActions(game, game.CurrentTurnPlayer).ToList();

        var techInteractionIds = await InteractionsHelper.SetUpInteractionsAsync(
            techActions.Select(x => new UseTechActionInteraction
            {
                Game = game.DocumentId,
                ForGamePlayerId = game.CurrentTurnPlayer.GamePlayerId,
                TechId = x.Tech.Id,
                ActionId = x.Id,
                UsingPlayerId = game.CurrentTurnPlayer.GamePlayerId
            }), interactionGroupId);
        
        await ShowBoardStateMessageAsync(builder, game);
        builder.AppendContentNewline("Your Turn".DiscordHeading2())
            .AppendContentNewline(game.ActionTakenThisTurn ?
                $"{name}, you have taken your main action this turn but you still have free actions from techs available. Select one or click 'End Turn'"
                : $"{name}, it is your turn. Choose an action:")
            .AllowMentions(game.CurrentTurnPlayer)
            .AppendContentNewline("Basic Actions:")
            .AddActionRowComponent(
                new DiscordButtonComponent(DiscordButtonStyle.Primary, moveInteractionId, "Move Action", game.ActionTakenThisTurn),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, produceInteractionId, "Produce Action", game.ActionTakenThisTurn),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, refreshInteractionId, "Refresh Action", game.ActionTakenThisTurn)

            );

        var techMainActions = techActions.Zip(techInteractionIds)
            .Where(x => x.First.ActionType == ActionType.Main)
            .ToList();
        if (techMainActions.Count > 0)
        {
            builder.AppendContentNewline("Tech Actions:")
                .AppendButtonRows(techMainActions
                        .Select(x => DiscordHelpers.CreateButtonForTechAction(x.First, x.Second)));
        }

        var techFreeActions = techActions.Zip(techInteractionIds)
            .Where(x => x.First.ActionType == ActionType.Free)
            .ToList();

        if (techFreeActions.Count > 0)
        {
            builder.AppendContentNewline("Free Tech Actions:")
                .AppendButtonRows(techFreeActions
                        .Select(x => DiscordHelpers.CreateButtonForTechAction(x.First, x.Second, DiscordButtonStyle.Secondary)));
        }
        
        builder.AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Danger, endTurnInteractionId, "End Turn"));

        return builder;
    }
    
    public static async Task<TBuilder?> OnActionCompletedAsync<TBuilder>(TBuilder? builder, Game game, ActionType actionType, IServiceProvider serviceProvider)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        if (actionType == ActionType.Main)
        {
            Debug.Assert(!game.ActionTakenThisTurn);
            game.ActionTakenThisTurn = true;
        }

        return await AdvanceTurnOrPromptNextActionAsync(builder, game, serviceProvider);
    }

    public static async Task<TBuilder?> AdvanceTurnOrPromptNextActionAsync<TBuilder>(TBuilder? builder, Game game, IServiceProvider serviceProvider)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        if (game.IsWaitingForTechPurchaseDecision)
        {
            return builder;
        }
        
        // If the player could still do something else, return to action selection
        if (!game.ActionTakenThisTurn || GetPlayerTechActions(game, game.CurrentTurnPlayer).Any(x => x.IsAvailable))
        {
            if (builder != null)
            {
                await ShowSelectActionMessageAsync(builder, game, serviceProvider);
            }
        }
        else
        {
            await NextTurnAsync(builder, game, serviceProvider);    
        }
        
        return builder;
    }

    /// <summary>
    /// Advances the game to the next turn
    /// </summary>
    public static async Task NextTurnAsync<TBuilder>(TBuilder? builder, Game game, IServiceProvider serviceProvider)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var endingTurnPlayer = game.CurrentTurnPlayer;
        endingTurnPlayer.LastTurnEvents.Clear();
        var currentTurnActions = endingTurnPlayer.CurrentTurnEvents.ToList();
        endingTurnPlayer.CurrentTurnEvents.Clear();
        endingTurnPlayer.LastTurnEvents.AddRange(currentTurnActions);

        foreach (var playerTech in endingTurnPlayer.Techs)
        {
            playerTech.UsedThisTurn = false;
        }
        
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
                builder?.AppendContentNewline($"**{name} scores and is now on {scoringPlayer.VictoryPoints}/6 VP!**")
                    .AllowMentions(scoringPlayer);

                await CheckForVictoryAsync(builder, game);
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
        game.ActionTakenThisTurn = false;
        
        if (game.Phase == GamePhase.Finished || builder == null)
        {
            return;
        }
        
        await ShowSelectActionMessageAsync(builder, game, serviceProvider);
    }

    public static async Task<TBuilder?> CheckForVictoryAsync<TBuilder>(TBuilder? builder, Game game) where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var winner = game.Players.FirstOrDefault(x => x.VictoryPoints == GameConstants.VpToWin);
        if (winner != null)
        {
            var name = await winner.GetNameAsync(true);
            builder?.AppendContentNewline($"{name} has won the game!".DiscordHeading1())
                .AllowMentions(winner)
                .AppendContentNewline("If you want to continue, fix up the game state so there is no longer a winner and use /turn_message to continue playing");
            game.Phase = GamePhase.Finished;
        }

        return builder;
    }

    /// <summary>
    /// Check if any players have been eliminated. Note that this can end the game.
    /// </summary>
    public static async Task CheckForPlayerEliminationsAsync<TBuilder>(TBuilder builder, Game game)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        foreach (var player in game.Players.Where(x => !x.IsEliminated))
        {
            if (game.Hexes.Any(x => x.Planet?.OwningPlayerId == player.GamePlayerId && x.ForcesPresent > 0))
            {
                continue;
            }

            player.IsEliminated = true;
            
            var name = await player.GetNameAsync(true);
            builder.AppendContentNewline($"{name} has been eliminated!".DiscordBold())
                .AllowMentions(player);

            if (game.ScoringTokenPlayer == player)
            {
                await CycleScoringTokenAsync(builder, game);
            }
        }

        var remainingPlayers = game.Players.Count(x => !x.IsEliminated); 
        if (remainingPlayers == 1)
        {
            var winner = game.Players.First(x => !x.IsEliminated);
            builder.AppendContentNewline($"{await winner.GetNameAsync(true)} wins the game through glorious violence by being the last one standing!".DiscordBold())
                .AllowMentions(winner);
            game.Phase = GamePhase.Finished;
        }
        else if (remainingPlayers == 0)
        {
            builder.AppendContentNewline("It would appear that @everyone has wiped each other out, leaving the universe cold and lifeless. Oops.".DiscordBold())
                .AddMention(EveryoneMention.All);
            game.Phase = GamePhase.Finished;
        }
    }

    public static void ResolveGameEvent(Game game, GameEvent gameEvent)
    {
        
    }

    public static IEnumerable<TriggeredEffect> GetTriggeredEffects(Game game, GameEvent gameEvent, GamePlayer player) =>
        player.Techs.SelectMany(x => Tech.TechsById[x.TechId].GetTriggeredEffects(game, gameEvent, player));

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
    
    public static IEnumerable<TechAction> GetPlayerTechActions(Game game, GamePlayer player)
        => player.Techs.SelectMany(x => Tech.TechsById[x.TechId].GetActions(game, player));
}