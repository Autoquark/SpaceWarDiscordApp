using System.Diagnostics;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.DatabaseModels;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Commands;

public class MoveActionCommands : IInteractionHandler<ShowMoveOptionsInteraction>,
    IInteractionHandler<BeginPlanningMoveActionInteraction>,
    IInteractionHandler<ShowSpecifyMovementAmountFromPlanetInteraction>,
    IInteractionHandler<SubmitSpecifyMovementAmountFromPlanetInteraction>,
    IInteractionHandler<PerformPlannedMoveInteraction>
{
    public async Task HandleInteractionAsync(ShowMoveOptionsInteraction interactionData, Game game, InteractionCreatedEventArgs args)
    {
        ISet<BoardHex> destinations = new HashSet<BoardHex>();
        foreach (var fromHex in game.Hexes.Where(x => x.Planet?.OwningPlayerId == interactionData.ForGamePlayerId))
        {
            destinations.UnionWith(BoardUtils.GetNeighbouringHexes(game, fromHex));
        }

        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);
        var playerName = await player.GetNameAsync(true);
        var messageBuilder = new DiscordWebhookBuilder()
            .EnableV2Components()
            .AppendContentNewline($"{playerName}, you may move to the following hexes: ");

        IDictionary<BoardHex, string> interactionIds = await Program.FirestoreDb.RunTransactionAsync(async transaction
            => destinations.ToDictionary(
                x => x,
                x => InteractionsHelper.SetUpInteraction(new BeginPlanningMoveActionInteraction
                {
                    Game = game.DocumentId,
                    Destination = x.Coordinates,
                    AllowedGamePlayerIds = player.IsDummyPlayer ? [] : [player.GamePlayerId],
                    MovingGamePlayerId = player.GamePlayerId,
                    EditOriginalMessage = true
                }, transaction))
            );
        
        foreach(var group in destinations.ZipWithIndices().GroupBy(x => x.Item2 / 5))
        {
            messageBuilder.AddActionRowComponent(
                group.Select(x => new DiscordButtonComponent(DiscordButtonStyle.Primary, interactionIds[x.Item1], x.Item1.Coordinates.ToString())));
        }
        
        await args.Interaction.EditOriginalResponseAsync(messageBuilder);
    }
    
    public async Task HandleInteractionAsync(BeginPlanningMoveActionInteraction interactionData, Game game, InteractionCreatedEventArgs args)
    {
        var destination = game.GetHexAt(interactionData.Destination);
        if (destination == null)
        {
            throw new Exception();
        }

        game.GetGamePlayerByGameId(interactionData.MovingGamePlayerId)
            .PlannedMove = new PlannedMove
            {
                Destination = interactionData.Destination
            };

        var player = game.GetGamePlayerByGameId(interactionData.MovingGamePlayerId);
        var sources = BoardUtils.GetStandardMoveSources(game, destination, player);
        var playerName = await player.GetNameAsync(true);

        if (!sources.Any())
        {
            throw new Exception();
        }

        var messageBuilder = new DiscordWebhookBuilder().EnableV2Components();
        ShowPlannedMove(messageBuilder, player);
        
        if (sources.Count == 1)
        {
            await Program.FirestoreDb.RunTransactionAsync(transaction =>
            {
                transaction.Set(game);
                return Task.CompletedTask;
            });
            // Only one planet we can move from, skip straight to specifying amount
            await ShowSpecifyMovementAmountButtonsAsync(messageBuilder, game, player, sources.Single(), destination);
        }
        else
        {
            var interactionsToSetUp = await ShowSpecifyMovementSourceButtonsAsync(messageBuilder, game, player, destination);
            await Program.FirestoreDb.RunTransactionAsync(transaction =>
            {
                // Save updated planned move data
                transaction.Set(game);

                InteractionsHelper.SetUpInteractions(interactionsToSetUp, transaction);

                return Task.CompletedTask;
            });
        }
        
        await args.Interaction.EditOriginalResponseAsync(messageBuilder);
    }

    public async Task HandleInteractionAsync(ShowSpecifyMovementAmountFromPlanetInteraction interactionData, Game game,
        InteractionCreatedEventArgs args)
    {
        var player = game.GetGamePlayerByGameId(interactionData.MovingPlayerId);

        if (player.PlannedMove == null)
        {
            throw new Exception();
        }
        
        await args.Interaction.EditOriginalResponseAsync(
            await ShowSpecifyMovementAmountButtonsAsync(
                new DiscordWebhookBuilder().EnableV2Components(),
                game,
                player,
                game.GetHexAt(interactionData.Source),
                game.GetHexAt(player.PlannedMove.Destination))
            );
    }

    public async Task<TBuilder> ShowSpecifyMovementAmountButtonsAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player, BoardHex source, BoardHex destination)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        builder.AppendContentNewline($"How many forces do you wish to move from {source.Coordinates} to {destination.Coordinates}?");

        if (source.Planet == null)
        {
            throw new Exception();
        }

        var interactionIds = await Program.FirestoreDb.RunTransactionAsync(async transaction
            => Enumerable.Range(0, source.Planet.ForcesPresent + 1).Select(x => InteractionsHelper.SetUpInteraction(
                new SubmitSpecifyMovementAmountFromPlanetInteraction
                {
                    Amount = x + 1,
                    From = source.Coordinates,
                    Game = game.DocumentId,
                    AllowedGamePlayerIds = player.IsDummyPlayer ? [] : [player.GamePlayerId],
                    MovingPlayerId = player.GamePlayerId,
                    EditOriginalMessage = true
                }, transaction))
            .ToList());
        
        foreach(var group in Enumerable.Range(0, source.Planet!.ForcesPresent).GroupBy(x => x / 5))
        {
            builder.AddActionRowComponent(
                group.Select(x => new DiscordButtonComponent(DiscordButtonStyle.Primary, interactionIds[x], x.ToString())));
        }

        return builder;
    }
    
    public async Task<List<ShowSpecifyMovementAmountFromPlanetInteraction>> ShowSpecifyMovementSourceButtonsAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player, BoardHex destination)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var sources = BoardUtils.GetStandardMoveSources(game, destination, player);
        if (sources.Count == 0)
        {
            throw new Exception();
        }
        
        var name = await player.GetNameAsync(true);
        builder.AppendContentNewline($"{name}, choose a planet to move forces from: ");
        builder.AddActionRowComponent();

        var interactionIds = sources.ToDictionary(x => x,
            x => new ShowSpecifyMovementAmountFromPlanetInteraction
            {
                Game = game.DocumentId,
                Source = x.Coordinates,
                AllowedGamePlayerIds = player.IsDummyPlayer ? [] : [player.GamePlayerId],
                MovingPlayerId = player.GamePlayerId,
                EditOriginalMessage = true
            });
            
        foreach(var group in sources.ZipWithIndices().GroupBy(x => x.Item2 / 5))
        {
            builder.AddActionRowComponent(
                group.Select(x => new DiscordButtonComponent(DiscordButtonStyle.Primary, interactionIds[x.Item1].InteractionId, x.Item1.Coordinates.ToString())));
        }
        
        return interactionIds.Values.ToList();
    }

    public async Task HandleInteractionAsync(SubmitSpecifyMovementAmountFromPlanetInteraction interactionData, Game game,
        InteractionCreatedEventArgs args)
    {
        var player = game.GetGamePlayerByGameId(interactionData.MovingPlayerId);

        var entry = player.PlannedMove!.Sources.FirstOrDefault(x => x.Source == interactionData.From);
        if (entry == null && interactionData.Amount > 0)
        {
            entry = new SourceAndAmount
            {
                Source = interactionData.From,
                Amount = interactionData.Amount
            };
            player.PlannedMove.Sources.Add(entry);
        }
        else if(interactionData.Amount > 0)
        {
            entry!.Amount = interactionData.Amount;
        }
        else if(entry != null)
        {
            player.PlannedMove.Sources.Remove(entry);
        }

        var destinationHex = game.GetHexAt(player.PlannedMove.Destination);
        Debug.Assert(destinationHex != null);
        
        var builder = new DiscordWebhookBuilder().EnableV2Components();
        
        List<InteractionData> interactions = new List<InteractionData>();
        // If this was the only place we could move from, and a nonzero amount was specified, perform the move now
        var sources = BoardUtils.GetStandardMoveSources(game, destinationHex, player);
        if (sources.Count == 1 && entry != null)
        {
            Debug.Assert(entry.Source == sources.First().Coordinates);
            await PerformPlannedMoveAsync(builder, game, player);
        }
        else
        {
            // Otherwise, go back to showing possible sources
            ShowPlannedMove(builder, player);
            interactions.AddRange(await ShowSpecifyMovementSourceButtonsAsync(builder, game, player, destinationHex));
            var confirmInteraction = new PerformPlannedMoveInteraction()
            {
                Game = game.DocumentId,
                PlayerId = player.GamePlayerId,
                AllowedGamePlayerIds = player.IsDummyPlayer ? [] : [player.GamePlayerId]
            };
            interactions.Add(confirmInteraction);
            builder.AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Success,
                confirmInteraction.InteractionId, "Confirm move"));
        }
        
        await Program.FirestoreDb.RunTransactionAsync(transaction =>
        {
            transaction.Set(game);
            InteractionsHelper.SetUpInteractions(interactions, transaction);
            return Task.CompletedTask;
        });
        
        await args.Interaction.EditOriginalResponseAsync(builder);
    }

    /// <summary>
    /// Display a summary of the given player's current planned move
    /// </summary>
    /// <returns></returns>
    public TBuilder ShowPlannedMove<TBuilder>(TBuilder builder, GamePlayer player)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var plannedMove = player.PlannedMove;
        if (plannedMove == null)
        {
            throw new Exception();
        }
        
        builder.AppendContentNewline($"Moving to {plannedMove.Destination}");
        
        foreach (var source in plannedMove.Sources)
        {
            builder.AppendContentNewline($"{source.Amount} from {source.Source}");
        }
        return builder;
    }

    private static async Task PerformPlannedMoveAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var plannedMove = player.PlannedMove;
        if (plannedMove == null)
        {
            throw new Exception();
        }
        
        var destinationHex = game.GetHexAt(player.PlannedMove!.Destination);
        if (destinationHex?.Planet == null)
        {
            throw new Exception();
        }
        
        var moverName = await player.GetNameAsync(true);

        // Stage 1: Subtract moving forces from each source planet and calculate total forces moving
        var totalMoving = 0;
        foreach (var source in plannedMove.Sources)
        {
            var sourceHex = game.GetHexAt(source.Source);
            if (sourceHex?.Planet == null || sourceHex.Planet.ForcesPresent < source.Amount)
            {
                throw new Exception();
            }

            sourceHex.Planet.SubtractForces(source.Amount);
            totalMoving += source.Amount;
            builder.AppendContentNewline($"Moving {source.Amount} from {source.Source}");
        }

        if (plannedMove.Sources.Count > 1)
        {
            builder.AppendContentNewline($"Moving a total of {totalMoving} forces");
        }

        // Stage 2: Resolve combat or merging with allied forces
        var totalPreCapacityLimit = totalMoving;
        if (destinationHex.Planet.OwningPlayerId == player.GamePlayerId || destinationHex.Planet.IsNeutral)
        {
            totalPreCapacityLimit += destinationHex.Planet.ForcesPresent;
        }
        else
        {
            var defenderName =
                await game.GetGamePlayerByGameId(destinationHex.Planet.OwningPlayerId).GetNameAsync(true);
            var combatLoss = Math.Min(totalMoving, destinationHex.Planet.ForcesPresent);
            totalPreCapacityLimit -= combatLoss;
            destinationHex.Planet.SubtractForces(combatLoss);

            builder.AppendContentNewline($"{moverName} and {defenderName} each lose {combatLoss} forces in combat");
        }

        // Stage 3: Apply planet capacity limit
        var totalPostCapacityLimit = Math.Min(GameConstants.MaxForcesPerPlanet, totalPreCapacityLimit);
        var lossToCapacityLimit = Math.Max(0, totalPreCapacityLimit - totalPostCapacityLimit);

        if (lossToCapacityLimit > 0)
        {
            builder.AppendContentNewline($"{moverName} lost {lossToCapacityLimit} forces that were exceeding the planet capacity");
        }
        
        // Stage 4: Save back to game state
        if (totalPostCapacityLimit > 0)
        {
            destinationHex.Planet.ForcesPresent = totalPostCapacityLimit;
            destinationHex.Planet.OwningPlayerId = player.GamePlayerId;
        }

        player.PlannedMove = null;

        builder.AppendContentNewline($"{moverName} now has {totalPostCapacityLimit} forces present on {destinationHex.Coordinates}");
        
        await GameplayCommands.NextTurnAsync(builder, game);
        await Program.FirestoreDb.RunTransactionAsync(async transaction => transaction.Set(game));
    }

    public async Task HandleInteractionAsync(PerformPlannedMoveInteraction interactionData, Game game, InteractionCreatedEventArgs args)
    {
        var builder = new DiscordFollowupMessageBuilder().EnableV2Components();
        await PerformPlannedMoveAsync(builder, game, game.GetGamePlayerByGameId(interactionData.PlayerId));
        await args.Interaction.DeleteOriginalResponseAsync();
        await args.Interaction.CreateFollowupMessageAsync(builder);
    }
}