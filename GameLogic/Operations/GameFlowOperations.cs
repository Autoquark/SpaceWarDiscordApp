using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using DSharpPlus.Entities;
using Google.Cloud.Firestore;
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

public class GameFlowOperations : IEventResolvedHandler<GameEvent_ActionComplete>
{
    public static async Task<DiscordMultiMessageBuilder> ShowBoardStateMessageAsync(DiscordMultiMessageBuilder builder, Game game)
    {
        using var image = BoardImageGenerator.GenerateBoardImage(game);
        var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;

        var name = await game.CurrentTurnPlayer.GetNameAsync(false);
        builder.NewMessage()
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

    public static async Task<DiscordMultiMessageBuilder> ShowSelectActionMessageAsync(DiscordMultiMessageBuilder builder, Game game, IServiceProvider serviceProvider)
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
        }, interactionGroupId);

        var endTurnInteractionId = await InteractionsHelper.SetUpInteractionAsync(new EndTurnInteraction
        {
            ForGamePlayerId = game.CurrentTurnPlayer.GamePlayerId,
            Game = game.DocumentId,
            EditOriginalMessage = false
        }, interactionGroupId);

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
            .WithAllowedMentions(game.CurrentTurnPlayer)
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

    public static async Task<DiscordMultiMessageBuilder?> OnActionCompletedAsync(DiscordMultiMessageBuilder? builder, Game game, ActionType actionType, IServiceProvider serviceProvider)
    {
        if (actionType == ActionType.Main)
        {
            Debug.Assert(!game.ActionTakenThisTurn);
            game.ActionTakenThisTurn = true;
        }

        return await AdvanceTurnOrPromptNextActionAsync(builder, game, serviceProvider);
    }

    public static async Task<DiscordMultiMessageBuilder?> AdvanceTurnOrPromptNextActionAsync(DiscordMultiMessageBuilder? builder, Game game, IServiceProvider serviceProvider)
    {
        if (game.IsWaitingForTechPurchaseDecision || game.EventStack.Items.Count > 0)
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
    public static async Task NextTurnAsync(DiscordMultiMessageBuilder? builder, Game game, IServiceProvider serviceProvider)
    {
        if (game.Phase == GamePhase.Finished)
        {
            return;
        }
        
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
                    .WithAllowedMentions(scoringPlayer);

                await CheckForVictoryAsync(builder, game);
            }

            // If someone appears to have won, still finish the end of turn logic (in case the game is fixed up and continued)
            // but don't post any messages about it.
            if (game.Phase == GamePhase.Finished)
            {
                builder = null;
            }
            await CycleScoringTokenAsync(builder, game);
        }

        do
        {
            game.CurrentTurnPlayerIndex = (game.CurrentTurnPlayerIndex + 1) % game.Players.Count;
        }
        while (game.CurrentTurnPlayer.IsEliminated);
        
        game.TurnNumber++;
        game.ActionTakenThisTurn = false;
        
        if (builder == null)
        {
            return;
        }
        
        await ShowSelectActionMessageAsync(builder, game, serviceProvider);
    }

    public static async Task<DiscordMultiMessageBuilder?> CheckForVictoryAsync(DiscordMultiMessageBuilder? builder, Game game)
    {
        var winner = game.Players.FirstOrDefault(x => x.VictoryPoints == GameConstants.VpToWin);
        if (winner != null)
        {
            var name = await winner.GetNameAsync(true);
            builder?.AppendContentNewline($"{name} has won the game!".DiscordHeading1())
                .WithAllowedMentions(winner)
                .AppendContentNewline("If you want to continue, fix up the game state so there is no longer a winner and use /turn_message to continue playing");
            game.Phase = GamePhase.Finished;
        }

        return builder;
    }

    /// <summary>
    /// Check if any players have been eliminated. Note that this can end the game.
    /// </summary>
    public static async Task<DiscordMultiMessageBuilder?> CheckForPlayerEliminationsAsync(DiscordMultiMessageBuilder? builder, Game game)
    {
        foreach (var player in game.Players.Where(x => !x.IsEliminated))
        {
            if (game.Hexes.Any(x => x.Planet?.OwningPlayerId == player.GamePlayerId && x.ForcesPresent > 0))
            {
                continue;
            }

            player.IsEliminated = true;
            
            var name = await player.GetNameAsync(true);
            builder?.AppendContentNewline($"{name} has been eliminated!".DiscordBold())
                .WithAllowedMentions(player);

            if (game.ScoringTokenPlayer == player)
            {
                await CycleScoringTokenAsync(builder, game);
            }
        }

        var remainingPlayers = game.Players.Count(x => !x.IsEliminated); 
        if (remainingPlayers == 1)
        {
            var winner = game.Players.First(x => !x.IsEliminated);
            builder?.AppendContentNewline($"{await winner.GetNameAsync(true)} is the last one standing, winning the game through glorious violence!".DiscordBold())
                .WithAllowedMentions(winner);
            game.Phase = GamePhase.Finished;
        }
        else if (remainingPlayers == 0)
        {
            builder?.AppendContentNewline("It would appear that @everyone has wiped each other out, leaving the universe cold and lifeless. Oops.".DiscordBold())
                .WithAllowedMentions(EveryoneMention.All);
            game.Phase = GamePhase.Finished;
        }
        
        return builder;
    }

    public static async Task PushGameEventsAsync(DiscordMultiMessageBuilder? builder, Game game,
        IServiceProvider serviceProvider, params IEnumerable<GameEvent> gameEvents)
    {
        foreach (var gameEvent in gameEvents.Reverse())
        {
            gameEvent.PlayerIdsToResolveTriggersFor = game.PlayersInTurnOrderFrom(game.CurrentTurnPlayer)
                .Select(x => x.GamePlayerId)
                .ToList();
            game.EventStack.Add(gameEvent);
        }
    }

    public static void TriggerResolved(Game game, string interactionId)
    {
        var triggerList = game.EventStack.LastOrDefault()?.RemainingTriggersToResolve;
        var triggeredEffect = triggerList?.Find(x => x.ResolveInteractionId == interactionId);
        if (triggeredEffect == null)
        {
            throw new Exception("Triggered effect not found or is not in response to top event on stack");
        }
        
        triggerList!.Remove(triggeredEffect);
    }

    public static async Task<DiscordMultiMessageBuilder?> ContinueResolvingEventStackAsync(DiscordMultiMessageBuilder? builder, Game game, IServiceProvider serviceProvider)
    {
        while (game.EventStack.Items.Count > 0)
        {
            var resolvingEvent = game.EventStack.Last();
            
            var autoResolveTrigger = resolvingEvent.RemainingTriggersToResolve.FirstOrDefault(x => x.AlwaysAutoResolve);
            if (autoResolveTrigger != null)
            {
                await ResolveTriggeredEffectAsync(builder, game, autoResolveTrigger, serviceProvider);
                
                continue;
            }
            
            // If there is only one trigger and it's mandatory, we can auto resolve it
            if (resolvingEvent.RemainingTriggersToResolve is [{ IsMandatory: true }])
            {
                var resolvingTrigger = resolvingEvent.RemainingTriggersToResolve[0];
                await ResolveTriggeredEffectAsync(builder, game, resolvingTrigger, serviceProvider);
                
                continue;
            }
            // Multiple and/or optional triggers, need to get a player decision
            else if (resolvingEvent.RemainingTriggersToResolve.Count > 0)
            {
                var player = game.GetGamePlayerByGameId(resolvingEvent.ResolvingTriggersForPlayerId);
                var name = await player.GetNameAsync(true);
                // TODO: Better messaging, different for if there are any mandatory or not
                var mandatoryCount = resolvingEvent.RemainingTriggersToResolve.Count(x => x.IsMandatory);
                var optionalCount = resolvingEvent.RemainingTriggersToResolve.Count - mandatoryCount;

                if (mandatoryCount == 0)
                {
                    builder?.AppendContentNewline(
                        $"{name}, you have optional tech effects which you may trigger. Please select one to resolve next or click 'Decline'.");
                }
                else if (mandatoryCount > 1 && optionalCount == 0)
                {
                    builder?.AppendContentNewline(
                        $"{name}, you may choose the order in which these mandatory tech effects resolve. Please select one to resolve next.");
                }
                else
                {
                    builder?.AppendContentNewline(
                        $"{name}, you have optional tech effects which you may trigger. There is also at least one mandatory effect which must be resolved before continuing. Please select an effect to resolve next.");
                }
                
                var interactionGroupId = serviceProvider.GetRequiredService<SpaceWarCommandContextData>().GlobalData
                    .InteractionGroupId;
                
                // Store or update interaction data for buttons in DB
                await Program.FirestoreDb.RunTransactionAsync(async transaction =>
                {
                    // For triggers whose InteractionData has already been saved to the DB, we need to update the 
                    // interaction group ID so AI players know they are still among the current available choices
                    var idsToUpdate = resolvingEvent.RemainingTriggersToResolve
                        .Select(x => x.ResolveInteractionId)
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList();

                    IReadOnlyList<DocumentSnapshot> toUpdate = new ReadOnlyCollection<DocumentSnapshot>([]);
                    if (idsToUpdate.Count > 0)
                    {
                        toUpdate = (await transaction.GetSnapshotAsync(
                            new Query<InteractionData>(transaction.Database.InteractionData())
                                .WhereIn(x => x.InteractionId, idsToUpdate))).Documents;
                    }
                    
                    foreach (var trigger in resolvingEvent.RemainingTriggersToResolve
                        .Where(x => x.ResolveInteractionData != null && string.IsNullOrEmpty(x.ResolveInteractionId)))
                    {
                        trigger.ResolveInteractionId =
                            InteractionsHelper.SetUpInteraction(trigger.ResolveInteractionData!, transaction,
                                interactionGroupId);
                    }

                    foreach (var document in toUpdate)
                    {
                        transaction.Update(document.Reference, nameof(InteractionData.InteractionGroupId), interactionGroupId);
                    }
                });
                
                builder?.AppendButtonRows(resolvingEvent.RemainingTriggersToResolve.Select(x =>
                    new DiscordButtonComponent(
                        x.IsMandatory ? DiscordButtonStyle.Primary : DiscordButtonStyle.Secondary,
                        x.ResolveInteractionId, x.DisplayName)));

                // If there are no mandatory triggers left, the player can decline remaining optional triggers
                if (resolvingEvent.RemainingTriggersToResolve.All(x => !x.IsMandatory))
                {
                    var interactionId = await InteractionsHelper.SetUpInteractionAsync(new DeclineOptionalTriggersInteraction
                    {
                        Game = game.DocumentId,
                        ForGamePlayerId = resolvingEvent.ResolvingTriggersForPlayerId
                    }, serviceProvider.GetRequiredService<SpaceWarCommandContextData>().GlobalData.InteractionGroupId);
                    builder?.AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Danger, interactionId, "Decline Optional Trigger(s)"));
                }

                break;
            }
            // No triggers left for this player
            else
            {
                // Move on to the next player
                if(resolvingEvent.PlayerIdsToResolveTriggersFor.Count > 0)
                {
                    var player = game.GetGamePlayerByGameId(resolvingEvent.PlayerIdsToResolveTriggersFor[0]);
                    resolvingEvent.PlayerIdsToResolveTriggersFor.RemoveAt(0);
                    resolvingEvent.ResolvingTriggersForPlayerId = player.GamePlayerId;
                    resolvingEvent.RemainingTriggersToResolve = GetTriggeredEffects(game, resolvingEvent, player).ToList();
                    
                    continue;
                }
                // Player choice event, display choices and stop resolving
                else if(resolvingEvent is GameEvent_PlayerChoice choiceEvent)
                {
                    if (builder != null)
                    {
                        await GameEventDispatcher.ShowPlayerChoicesForEvent(builder, choiceEvent, game,
                            serviceProvider);
                    }

                    break;
                }
                // No more players to resolve, pop this event from the stack and resolve its OnResolve
                else
                {
                    await PopEventFromStackAndResolveAsync(builder, game, serviceProvider);
                    continue;
                }

                // Event requires explicit resolve, pause resolution
                break;
            }
        }
        
        return (await AdvanceTurnOrPromptNextActionAsync(builder, game, serviceProvider))!;
    }

    public static async Task<DiscordMultiMessageBuilder?> DeclineOptionalTriggersAsync(DiscordMultiMessageBuilder? builder,
        Game game, IServiceProvider serviceProvider)
    {
        var gameEvent = game.EventStack.LastOrDefault();
        if (gameEvent == null)
        {
            return builder;
        }

        if (gameEvent.RemainingTriggersToResolve.Any(x => x.IsMandatory))
        {
            Debug.Assert(false);
            return builder;
        }
        
        gameEvent.RemainingTriggersToResolve.Clear();
        
        return (await ContinueResolvingEventStackAsync(builder, game, serviceProvider))!;
    }

    private static async Task<DiscordMultiMessageBuilder?> PopEventFromStackAndResolveAsync(DiscordMultiMessageBuilder? builder, Game game,
        IServiceProvider serviceProvider)
    {
        var resolving = game.EventStack.LastOrDefault();
        if (resolving == null)
        {
            throw new Exception("No events to resolve");
        }
        
        game.EventStack.RemoveAt(game.EventStack.Items.Count - 1);
        await GameEventDispatcher.HandleEventResolvedAsync(builder, resolving, game, serviceProvider);
        
        return builder;
    }

    public static IEnumerable<TriggeredEffect> GetTriggeredEffects(Game game, GameEvent gameEvent, GamePlayer player)
    {
        var triggers = player.Techs.SelectMany(x => Tech.TechsById[x.TechId].GetTriggeredEffects(game, gameEvent, player))
            .ToList();

        foreach (var triggeredEffect in triggers)
        {
            triggeredEffect.ResolveInteractionId = triggeredEffect.ResolveInteractionData!.InteractionId;
        }
        
        return triggers;
    }

    /// <summary>
    /// Passes the scoring token to the previous valid player in turn order
    /// </summary>
    private static async Task CycleScoringTokenAsync(DiscordMultiMessageBuilder? builder, Game game)
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

    private static async Task<DiscordMultiMessageBuilder?> ResolveTriggeredEffectAsync(DiscordMultiMessageBuilder? builder, Game game,
        TriggeredEffect triggeredEffect, IServiceProvider serviceProvider)
    {
        var interactionData = triggeredEffect.ResolveInteractionData
            ?? await Program.FirestoreDb.RunTransactionAsync(async transaction => await transaction.GetInteractionDataAsync<TriggeredEffectInteractionData>(Guid.Parse(triggeredEffect.ResolveInteractionId)));

        if (interactionData == null)
        {
            throw new Exception("Interaction data not found");
        }

        await InteractionDispatcher.HandleInteractionAsync(builder, interactionData, game, serviceProvider);
        return builder;
    }
    
    public static IEnumerable<TechAction> GetPlayerTechActions(Game game, GamePlayer player)
        => player.Techs.SelectMany(x => Tech.TechsById[x.TechId].GetActions(game, player));

    public async Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent_ActionComplete gameEvent, Game game,
        IServiceProvider serviceProvider)
        => await OnActionCompletedAsync(builder, game, gameEvent.ActionType, serviceProvider);

    public static void OnUserTriggeredResolveEffectInteraction(Game game, TriggeredEffectInteractionData interactionData)
    {
        var triggerList = game.EventStack.LastOrDefault()?.RemainingTriggersToResolve;
        var triggeredEffect = triggerList?.Find(x => x.ResolveInteractionId == interactionData.InteractionId);
        if (triggeredEffect == null)
        {
            throw new Exception("Triggered effect not found or is not in response to top event on stack");
        }
        
        triggerList!.Remove(triggeredEffect);
    }
}