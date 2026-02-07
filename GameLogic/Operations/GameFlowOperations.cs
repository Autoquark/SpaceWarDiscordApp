using System.Collections.ObjectModel;
using System.Diagnostics;
using DSharpPlus.Entities;
using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Setup;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.Move;
using SpaceWarDiscordApp.Database.InteractionData.Tech;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.MapGeneration;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public class GameFlowOperations : IEventResolvedHandler<GameEvent_TurnBegin>, IEventResolvedHandler<GameEvent_ActionComplete>, IInteractionHandler<StartGameInteraction>,
    IEventResolvedHandler<GameEvent_PostForcesDestroyed>, IInteractionHandler<ShowBoardInteraction>
{
    public static async Task<DiscordMultiMessageBuilder> ShowSelectActionMessageAsync(DiscordMultiMessageBuilder builder, Game game, IServiceProvider serviceProvider)
    {
        var transientState = serviceProvider.GetRequiredService<PerOperationGameState>(); 
        if (transientState.HavePrintedSelectActionMessage || game.Phase == GamePhase.Finished)
        {
            return builder;
        }
        
        transientState.HavePrintedSelectActionMessage = true;
        
        var name = await game.CurrentTurnPlayer.GetNameAsync(true);
        
        var moveInteractionId = serviceProvider.AddInteractionToSetUp(new BeginPlanningMoveInteraction<MoveActionCommands>()
        {
            Game = game.DocumentId,
            ForGamePlayerId = game.CurrentTurnPlayer.GamePlayerId,
        });

        var produceInteractionId = serviceProvider.AddInteractionToSetUp(new ShowProduceOptionsInteraction
        {
            Game = game.DocumentId,
            ForGamePlayerId = game.CurrentTurnPlayer.GamePlayerId,
        });

        var refreshInteractionId = serviceProvider.AddInteractionToSetUp(new RefreshActionInteraction
        {
            Game = game.DocumentId,
            ForGamePlayerId = game.CurrentTurnPlayer.GamePlayerId,
        });
        
        // If this is the start of the turn, don't show the rollback state we just created
        var relevantRollbacks = (game.AnyActionTakenThisTurn
                ? game.RollbackStates
                : game.RollbackStates.Where(x => x.TurnNumber < game.TurnNumber))
            .Reverse()
            .ToList();

        var rollbackInteractionIds = serviceProvider.AddInteractionsToSetUp(relevantRollbacks.Select(x =>
            new ShowRollBackConfirmInteraction
            {
                BackupGameDocument = x.GameDocument,
                Game = game.DocumentId,
                // Anyone can trigger a rollback e.g. previous player might want to redo their turn
                ForGamePlayerId = -1
            }));

        var endTurnInteractionId = serviceProvider.AddInteractionToSetUp(new EndTurnInteraction
        {
            ForGamePlayerId = game.CurrentTurnPlayer.GamePlayerId,
            Game = game.DocumentId,
            EditOriginalMessage = false
        });

        var techActions = GetPlayerTechActions(game, game.CurrentTurnPlayer).ToList();

        var techInteractionIds = serviceProvider.AddInteractionsToSetUp(
            techActions.Select(x => new UseTechActionInteraction
            {
                Game = game.DocumentId,
                ForGamePlayerId = game.CurrentTurnPlayer.GamePlayerId,
                TechId = x.Tech.Id,
                ActionId = x.Id,
                UsingPlayerId = game.CurrentTurnPlayer.GamePlayerId
            })).ToList();
        
        await GameStateOperations.ShowBoardStateMessageAsync(builder, game);
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
        builder.AppendButtonRows(relevantRollbacks.Zip(rollbackInteractionIds)
                .Select(x => 
                    new DiscordButtonComponent(DiscordButtonStyle.Danger,
                        x.Second,
                        $"Roll back to start of turn {x.First.TurnNumber} ({game.GetGamePlayerByGameId(x.First.CurrentTurnGamePlayerId).GetNameAsync(false, false).GetAwaiter().GetResult()}'s turn)")));

        return builder;
    }

    public static async Task<DiscordMultiMessageBuilder?> AdvanceTurnOrPromptNextActionAsync(DiscordMultiMessageBuilder? builder, Game game, IServiceProvider serviceProvider)
    {
        if (game.EventStack.Count > 0)
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

        foreach (var playerTech in endingTurnPlayer.Techs)
        {
            playerTech.UsedThisTurn = false;
        }

        switch (game.Rules.ScoringRule)
        {
            case ScoringRule.MostStars:
                if (game.IsScoringTurn)
                {
                    List<(GamePlayer player, int score)> playerScores = game.Players.Where(x => !x.IsEliminated)
                        .Select(x => (x, GameStateOperations.GetPlayerStars(game, x)))
                        .OrderByDescending(x => x.Item2)
                        .ToList();

                    if (playerScores[1].score < playerScores[0].score)
                    {
                        var scoringPlayer = playerScores[0].player;

                        // In a 2 player game, you can only score at the end of your opponent's turn
                        if (game.Players.Count > 2 || scoringPlayer != endingTurnPlayer)
                        {
                            scoringPlayer.VictoryPoints++;
                            var name = await scoringPlayer.GetNameAsync(true);
                            builder?.AppendContentNewline(
                                    $"**{name} scores and is now on {scoringPlayer.VictoryPoints}/6 VP!**")
                                .WithAllowedMentions(scoringPlayer);

                            await CheckForVictoryAsync(builder, game, serviceProvider);
                        }
                        else
                        {
                            builder?.AppendContentNewline(
                                "Nobody scores this turn (in a 2 player game, you can only score at the end of your opponent's turn)");
                        }
                    }
                    else
                    {
                        var drawnPlayerNames = await Task.WhenAll(playerScores
                            .TakeWhile(x => x.score == playerScores[0].score)
                            .Select(async x => await x.player.GetNameAsync(false)));
                        builder?.AppendContentNewline(
                            $"Nobody scores this turn (draw between {string.Join(", ", drawnPlayerNames)})");
                    }

                    // If someone appears to have won, still finish the end of turn logic (in case the game is fixed up and continued)
                    // but don't post any messages about it.
                    if (game.Phase == GamePhase.Finished)
                    {
                        builder = null;
                    }

                    await CycleScoringTokenAsync(builder, game);
                }

                break;

            case ScoringRule.Cumulative:
                var stars = GameStateOperations.GetPlayerStars(game, endingTurnPlayer);
                endingTurnPlayer.VictoryPoints += stars;

                builder?.AppendContentNewline(
                    $"{await endingTurnPlayer.GetNameAsync(false)} gains {stars} VP and now has {endingTurnPlayer.VictoryPoints}/{game.Rules.VictoryThreshold} VP");
                
                await CheckForVictoryAsync(builder, game, serviceProvider);
                break;
        }

        do
        {
            // Clear old event records even for eliminated players
            game.CurrentTurnPlayer.LastTurnEvents.Clear();
            game.CurrentTurnPlayer.LastTurnEvents.Clear();
            var currentTurnActions = game.CurrentTurnPlayer.CurrentTurnEvents.ToList();
            game.CurrentTurnPlayer.CurrentTurnEvents.Clear();
            game.CurrentTurnPlayer.LastTurnEvents.AddRange(currentTurnActions);
            
            game.CurrentTurnPlayerIndex = (game.CurrentTurnPlayerIndex + 1) % game.Players.Count;
        }
        while (game.CurrentTurnPlayer.IsEliminated);
        
        // New turn start
        game.TurnNumber++;
        
        // Cleanup actions taken last turn
        game.ActionTakenThisTurn = false;
        game.AnyActionTakenThisTurn = false;
        
        // Issue Turn Begin event
        await PushGameEventsAndResolveAsync(builder, game, serviceProvider, new GameEvent_TurnBegin
        {
            PlayerGameId = game.CurrentTurnPlayer.GamePlayerId,
        });
    }
    
    public async Task<DiscordMultiMessageBuilder> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent_TurnBegin gameEvent, Game game,
        IServiceProvider serviceProvider)
    {
        game.LastTurnProdTime = DateTime.UtcNow;
        ProdOperations.UpdateProdTimers(game, serviceProvider.GetRequiredService<SpaceWarCommandContextData>().NonDbGameState!);
        
        await GameManagementOperations.SaveRollbackStateAsync(game);
        return await ShowSelectActionMessageAsync(builder, game, serviceProvider);
    }

    public static async Task<DiscordMultiMessageBuilder?> CheckForVictoryAsync(DiscordMultiMessageBuilder? builder, Game game, IServiceProvider serviceProvider)
    {
        var winner = game.Players.FirstOrDefault(x => x.VictoryPoints >= game.Rules.VictoryThreshold);
        if (winner != null)
        {
            var name = await winner.GetNameAsync(true);
            builder?.NewMessage()
                .AppendContentNewline($"{name} has won the game!".DiscordHeading1())
                .AppendContentNewline($"({string.Join(", ", await Task.WhenAll(game.Players.Select(x => x.GetNameAsync(true))))})")
                .AppendContentNewline("If you want to continue, fix up the game state so there is no longer a winner and use /reprompt to continue playing")
                .WithAllowedMentions(game.Players);
            game.Phase = GamePhase.Finished;
            
            // Cancel any active prod timers
            ProdOperations.UpdateProdTimers(game, serviceProvider.GetRequiredService<SpaceWarCommandContextData>().NonDbGameState!);

            if (builder != null)
            {
                await GameStateOperations.ShowBoardStateMessageAsync(builder, game);
            }
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
            builder?.AppendContentNewline($"{name} has been eliminated!".DiscordHeading2())
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

    /// <summary>
    /// Pushes a sequence of game events onto the stack and continue resolving. Events will resolve in the order they were supplied
    /// </summary>
    public static async Task<DiscordMultiMessageBuilder?> PushGameEventsAndResolveAsync(DiscordMultiMessageBuilder? builder, Game game,
        IServiceProvider serviceProvider, params IEnumerable<GameEvent> gameEvents)
    {
        PushGameEvents(game, gameEvents);
        return await ContinueResolvingEventStackAsync(builder, game, serviceProvider);
    }
    
    /// <summary>
    /// Pushes a sequence of game events onto the stack. Events will resolve in the order they were supplied
    /// </summary>
    public static void PushGameEvents(Game game, params IEnumerable<GameEvent> gameEvents)
    {
        foreach (var gameEvent in gameEvents.Reverse())
        {
            gameEvent.PlayerIdsToResolveTriggersFor = game.PlayersInTurnOrderFrom(game.CurrentTurnPlayer)
                .Select(x => x.GamePlayerId)
                .ToList();
            game.EventStack.Add(gameEvent);
        }
    }

    public static async Task<DiscordMultiMessageBuilder?> PlayerChoiceEventResolvedAsync(Game game,
        DiscordMultiMessageBuilder? builder, IServiceProvider serviceProvider, string eventId)
    {
        var relevantEvent = game.EventStack.FirstOrDefault(x => x.EventId == eventId);
        if (relevantEvent is not GameEvent_PlayerChoice)
        {
            throw new Exception("Event not found or is not a player choice event");
        }
        
        game.EventStack.Remove(relevantEvent);
        return await ContinueResolvingEventStackAsync(builder, game, serviceProvider);
    }

    public static async Task<DiscordMultiMessageBuilder?> TriggerResolvedAsync(Game game, DiscordMultiMessageBuilder? builder, IServiceProvider serviceProvider, string interactionId)
    {
        // The interaction might not be for the top event on the stack if, in the process of resolving it, we pushed events onto the stack
        var relevantEvent = game.EventStack.FirstOrDefault(x =>
            x.RemainingTriggersToResolve.Any(y => y.ResolveInteractionId == interactionId));
        
        var triggeredEffect = relevantEvent?.RemainingTriggersToResolve.Find(x => x.ResolveInteractionId == interactionId);
        if (triggeredEffect == null)
        {
            throw new Exception("Triggered effect not found or is not in response to top event on stack");
        }
        
        relevantEvent!.TriggerIdsResolved.Add(triggeredEffect.TriggerId);
        
        // Reevaluate triggers for currently resolving player as this trigger may have changed the game state and caused
        // triggers to become available or unavailable. We use GameEvent.TriggerIdsResolved to ensure that we don't resolve
        // the same triggered effect multiple times for the same event.
        var resolvingPlayer = game.GetGamePlayerByGameId(relevantEvent.ResolvingTriggersForPlayerId);
        relevantEvent.RemainingTriggersToResolve = GetTriggeredEffects(game, relevantEvent, resolvingPlayer).ToList();
        
        serviceProvider.AddInteractionsToSetUp(relevantEvent.RemainingTriggersToResolve
            .Select(x => x.ResolveInteractionData).WhereNonNull());
        
        return await ContinueResolvingEventStackAsync(builder, game, serviceProvider);
    }

    public static async Task<DiscordMultiMessageBuilder?> ContinueResolvingEventStackAsync(DiscordMultiMessageBuilder? builder, Game game, IServiceProvider serviceProvider)
    {
        var transientState = serviceProvider.GetRequiredService<PerOperationGameState>();
        if (transientState.IsResolvingStack)
        {
            return builder;
        }
        
        transientState.IsResolvingStack = true;
        
        while (game.EventStack.Count > 0)
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

                var secondRowButtons = new List<DiscordButtonComponent>();
                // If there are no mandatory triggers left, the player can decline remaining optional triggers
                if (resolvingEvent.RemainingTriggersToResolve.All(x => !x.IsMandatory))
                {
                    var interactionId = serviceProvider.AddInteractionToSetUp(new DeclineOptionalTriggersInteraction
                    {
                        Game = game.DocumentId,
                        ForGamePlayerId = resolvingEvent.ResolvingTriggersForPlayerId
                    });
                    secondRowButtons.Add(new DiscordButtonComponent(DiscordButtonStyle.Danger, interactionId, "Decline Optional Trigger(s)"));
                }

                var showBoardId = serviceProvider.AddInteractionToSetUp(new ShowBoardInteraction
                {
                    ForGamePlayerId = -1,
                    Game = game.DocumentId
                });
                
                secondRowButtons.Add(DiscordHelpers.CreateShowBoardButton(showBoardId));
                builder?.AddActionRowComponent(secondRowButtons);

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

                    serviceProvider.AddInteractionsToSetUp(resolvingEvent.RemainingTriggersToResolve
                        .Select(x => x.ResolveInteractionData).WhereNonNull());
                    
                    continue;
                }
                // Player choice event, display choices and stop resolving
                else if(resolvingEvent is GameEvent_PlayerChoice choiceEvent)
                {
                    if (builder != null)
                    {
                        await GameEventDispatcher.ShowPlayerChoicesForEvent(builder, choiceEvent, game,
                            serviceProvider);

                        // If showing the choices caused the stack to change, continue resolving
                        if (game.EventStack.Count == 0 || game.EventStack[^1] != resolvingEvent)
                        {
                            continue;
                        }
                        
                        break;
                    }
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
        
        transientState.IsResolvingStack = false;
        
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
        
        game.EventStack.RemoveAt(game.EventStack.Count - 1);
        await GameEventDispatcher.HandleEventResolvedAsync(builder, resolving, game, serviceProvider);
        
        return builder;
    }

    public static IEnumerable<TriggeredEffect> GetTriggeredEffects(Game game, GameEvent gameEvent, GamePlayer player)
    {
        var triggers = player.Techs.SelectMany(x => Tech.TechsById[x.TechId].GetTriggeredEffects(game, gameEvent, player))
            .Where(x => !gameEvent.TriggerIdsResolved.Contains(x.TriggerId))
            .ToList();

        foreach (var triggeredEffect in triggers)
        {
            triggeredEffect.ResolveInteractionId = triggeredEffect.ResolveInteractionData!.InteractionId;
        }
        
        return triggers;
    }

    public static async Task<DiscordThreadChannel> GetOrCreateChatThreadAsync(Game game)
    {
        DiscordThreadChannel? chatThread = null;

        if (game.ChatThreadId != 0)
        {
            chatThread = (DiscordThreadChannel)await Program.DiscordClient.GetChannelAsync(game.ChatThreadId);
        }

        if (chatThread! == null!)
        {
            var gameChannel = await Program.DiscordClient.GetChannelAsync(game.GameChannelId);
            
            chatThread = await gameChannel.CreateThreadAsync("Game Chat", DiscordAutoArchiveDuration.Week, DiscordChannelType.PublicThread);

            foreach (var player in game.Players.Where(x => !x.IsDummyPlayer))
            {
                await chatThread.AddThreadMemberAsync(await gameChannel.Guild.GetMemberAsync(player.DiscordUserId));
            }
            
            await chatThread.SendMessageAsync("You can use this thread to talk to the other players without bot messages getting in the way.");
        }
        
        return chatThread;
    }
    
    public static async Task<DiscordThreadChannel> GetOrCreatePlayerPrivateThreadAsync(Game game, GamePlayer player)
    {
        DiscordThreadChannel? privateThread = null;
        
        if (player.PrivateThreadId != 0)
        {
            privateThread = (DiscordThreadChannel)await Program.DiscordClient.GetChannelAsync(player.PrivateThreadId);
        }

        if (privateThread! == null!)
        {
            var gameChannel = await Program.DiscordClient.GetChannelAsync(game.GameChannelId);
            var playerName = await player.GetNameAsync(false, false);

            // Dummy players have a public 'private' thread
            privateThread = await gameChannel.CreateThreadAsync($"{playerName}'s private thread",
                DiscordAutoArchiveDuration.Week, player.IsDummyPlayer ? DiscordChannelType.PublicThread : DiscordChannelType.PrivateThread);

            if (!player.IsDummyPlayer)
            {
                var playerMember = await gameChannel.Guild.GetMemberAsync(player.DiscordUserId);
                await privateThread.AddThreadMemberAsync(playerMember);
            }

            await privateThread.SendMessageAsync($"{await player.GetNameAsync(true)}, this is your private thread for {game.Name}, visible only to you (and server admins, but they promise not to cheat). Any secret information or choices will be presented here.");

            player.PrivateThreadId = privateThread.Id;
        }

        return privateThread;
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
    {
        if (gameEvent.ActionType == ActionType.Main)
        {
            Debug.Assert(!game.ActionTakenThisTurn);
            game.ActionTakenThisTurn = true;
        }
        
        game.AnyActionTakenThisTurn = true;
        game.LatestUnfinishedTurnProdTime = DateTime.UtcNow;
        ProdOperations.UpdateProdTimers(game, serviceProvider.GetRequiredService<SpaceWarCommandContextData>().NonDbGameState!);
        
        return await AdvanceTurnOrPromptNextActionAsync(builder, game, serviceProvider);
    }

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
    
    public static async Task<DiscordMultiMessageBuilder?> PlayerChooseStartingTechAsync(DiscordMultiMessageBuilder? builder, Game game, GamePlayer player, string techId, IServiceProvider serviceProvider)
    {
        if (game.Rules.StartingTechRule is StartingTechRule.OneUniversal or StartingTechRule.IndividualDraft)
        {
            player.StartingTechs = [techId];
        }
        
        // The button clicked might have been in a private thread, so explicitly use the game channel builder
        var gameBuilder = serviceProvider.GetRequiredService<GameMessageBuilders>().GameChannelBuilder!;
        
        var notChosenCount = game.Players.Count(x => x.StartingTechs.Count == 0);
        if (notChosenCount == 0)
        {
            foreach (var eachPlayer in game.Players)
            {
                gameBuilder.AppendContentNewline($"{await eachPlayer.GetNameAsync(false)}'s starting tech:");
                foreach (var tech in eachPlayer.StartingTechs.ToTechsById())
                {
                    TechOperations.ShowTechDetails(gameBuilder, tech.Id);
                    eachPlayer.Techs.Add(tech.CreatePlayerTech(game, eachPlayer));
                }
            }

            if (game.StartingTechHands.Count > 0)
            {
                var unchosen = game.StartingTechHands.SelectMany(x => x.Techs)
                    .Except(game.Players.SelectMany(x => x.StartingTechs))
                    .ToList();
                
                game.TechDiscards.AddRange(unchosen);
                
                gameBuilder.AppendContentNewline("The following techs were not chosen and have been discarded: " +
                                                 string.Join(", ", unchosen
                                                     .ToTechsById()
                                                     .Select(x => x.DisplayName)));
            }

            await TechOperations.UpdatePinnedTechMessage(game);
        }
        else
        {
            gameBuilder.AppendContentNewline(await player.GetNameAsync(false) + $" has chosen their starting tech (waiting for {notChosenCount} more players)");
        }

        return builder;
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, StartGameInteraction interactionData, Game game,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        await StartGameAsync(builder, game, serviceProvider);
        
        return new SpaceWarInteractionOutcome(true);
    }

    public async static Task<DiscordMultiMessageBuilder> StartGameAsync(DiscordMultiMessageBuilder builder, Game game, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        if (game.Players.Count <= 1)
        {
            return builder.AppendContentNewline("Not enough players");
        }

        if (game.Phase != GamePhase.Setup)
        {
            return builder.AppendContentNewline("Game has already started");
        }
        
        var mapGenerator = BaseMapGenerator.GetGenerator(game.Rules.MapGeneratorId);
        if (!mapGenerator.SupportedPlayerCounts.Contains(game.Players.Count))
        {
            return builder.AppendContentNewline("Can't start the game as selected map generator does not support the current player count");
        }
        
        // Shuffle turn order - this is also the map slice order
        game.Players = game.Players.Shuffled().ToList();
        
        game.Hexes = mapGenerator.GenerateMap(game);

        game.TechDeck = Tech.TechsById.Values.Where(x => x.ShouldIncludeInGame(game))
            .Select(x => x.Id)
            .ToList();
        game.TechDeck.Shuffle();

        // Select universal techs at random
        for (var i = 0; i < GameConstants.UniversalTechCount; i++)
        {
            // Draw cards until we get a suitable universal tech
            var drawn = new List<Tech>();
            do
            {
                drawn.Add(TechOperations.DrawTechFromDeckSilent(game)!);
            } while (!drawn[^1].CanBeUniversalForGame(game));
            
            game.UniversalTechs.Add(drawn[^1].Id);
            
            // Return unsuitable cards drawn to the deck and shuffle
            game.TechDeck.AddRange(drawn[0..^1].Select(x => x.Id));
            game.TechDeck.Shuffle();
        }

        for (var i = 0; i < GameConstants.MarketTechCount - 1; i++)
        {
            game.TechMarket.Add(TechOperations.DrawTechFromDeckSilent(game)!.Id);
        }
        
        game.TechMarket.Add(null);
        
        game.ScoringTokenPlayerIndex = game.Players.Count - 1;
        game.Phase = GamePhase.Play;
        
        builder.AppendContentNewline("The game has started.");
        builder.AppendContentNewline("Universal Techs:".DiscordHeading2());
        foreach (var tech in game.UniversalTechs)
        {
            TechOperations.ShowTechDetails(builder, tech);
        }
        
        builder.AppendContentNewline("Tech Market:".DiscordHeading2());
        foreach (var tech in game.TechMarket.WhereNonNull())
        {
            TechOperations.ShowTechDetails(builder, tech);
        }

        await TechOperations.UpdatePinnedTechMessage(game);
        
        builder.NewMessage();
        builder.AppendContentNewline("The Story So Far".DiscordHeading1());
        var backstoryGenerator = serviceProvider.GetRequiredService<BackstoryGenerator>();
        builder.AppendContentNewline(backstoryGenerator.GenerateBackstory(game));

        if (game.Players.Count == 2)
        {
            builder.NewMessage();
            builder.AppendContentNewline(
                "Reminder: In a 2 player game, you score if you have more stars at the end of your opponent's turn".DiscordHeading2());
        }

        if (game.Rules.StartingTechRule != StartingTechRule.None)
        {
            PushGameEvents(game, new GameEvent_PlayersChooseStartingTech());

            switch (game.Rules.StartingTechRule)
            {
                case StartingTechRule.IndividualDraft:
                    foreach (var (gamePlayer, index) in game.Players.ZipWithIndices())
                    {
                        var hand = new List<string>();
                        game.StartingTechHands.Add(new StartingTechHand
                        {
                            Techs = hand
                        });
                        for (var i = 0; i < 3; i++)
                        {
                            hand.Add(TechOperations.DrawTechFromDeckSilent(game)!.Id);
                        }
                        
                        gamePlayer.CurrentStartingTechHandIndex = index;
                    }
                    break;
            }
        }
        
        // Create the chat thread
        await GetOrCreateChatThreadAsync(game);
        
        return (await ContinueResolvingEventStackAsync(builder, game, serviceProvider))!;
    }

    public async Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent_PostForcesDestroyed gameEvent, Game game,
        IServiceProvider serviceProvider)
    {
        await CheckForPlayerEliminationsAsync(builder, game);
        return builder;
    }

    /// <summary>
    /// Pushes an event onto the stack to destroy the given amount of forces from the given hex
    /// </summary>
    public static int DestroyForces(Game game, BoardHex location, int amount, int responsiblePlayerId, ForcesDestructionReason reason, string? techId = null)
    {
        if ((reason == ForcesDestructionReason.Tech) == (techId == null))
        {
            throw new ArgumentException("Invalid combination of reason and techId");
        }
        
        if (amount > 0)
        {
            amount = Math.Min(amount, location.ForcesPresent);
            PushGameEvents(game, new GameEvent_PostForcesDestroyed
            {
                Amount = amount,
                Location = location.Coordinates,
                OwningPlayerGameId = location.Planet!.OwningPlayerId,
                ResponsiblePlayerGameId = responsiblePlayerId,
                Reason = reason,
                TechId = techId
            });
            location.Planet!.SubtractForces(amount);
        }

        return amount;
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, ShowBoardInteraction interactionData, Game game,
        IServiceProvider serviceProvider)
    {
        if (builder != null)
        {
            await GameStateOperations.ShowBoardStateMessageAsync(builder, game);
        }
        return new SpaceWarInteractionOutcome(false);
    }
}