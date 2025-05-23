using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.Move;
using SpaceWarDiscordApp.Discord;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public static class MovementOperations
{
    public static async Task PerformPlannedMoveAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var plannedMove = player.PlannedMove;
        if (plannedMove == null)
        {
            throw new Exception();
        }

        await ResolveMoveAsync(builder, game, player, plannedMove);
        
        await GameFlowOperations.MarkActionTakenForTurn(game.Phase == GamePhase.Finished ? null : builder, game);
    }

    /// <summary>
    /// Display a summary of the given player's current planned move
    /// </summary>
    public static TBuilder ShowPlannedMove<TBuilder>(TBuilder builder, GamePlayer player)
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

    public static async Task<List<ShowSpecifyMovementAmountFromPlanetInteraction>> ShowSpecifyMovementSourceButtonsAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player, BoardHex destination)
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
                ForGamePlayerId = player.GamePlayerId,
                EditOriginalMessage = true
            });
            
        builder.AppendButtonRows(
            sources.Select(x => DiscordHelpers.CreateButtonForHex(game, x, interactionIds[x].InteractionId)));
        
        return interactionIds.Values.ToList();
    }

    public static async Task<TBuilder> ShowSpecifyMovementAmountButtonsAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player, BoardHex source, BoardHex destination)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        builder.AppendContentNewline($"How many forces do you wish to move from {source.Coordinates} to {destination.Coordinates}?");

        if (source.Planet == null)
        {
            throw new Exception();
        }

        var interactionIds = await Program.FirestoreDb.RunTransactionAsync(transaction
            => Enumerable.Range(0, source.Planet.ForcesPresent + 1).Select(x => InteractionsHelper.SetUpInteraction(
                    new SubmitSpecifyMovementAmountFromPlanetInteraction
                    {
                        Amount = x,
                        From = source.Coordinates,
                        Game = game.DocumentId,
                        ForGamePlayerId = player.GamePlayerId,
                        EditOriginalMessage = true
                    }, transaction))
                .ToList());

        builder.AppendButtonRows(Enumerable.Range(0, source.Planet!.ForcesPresent + 1).Select(x =>
            new DiscordButtonComponent(DiscordButtonStyle.Primary, interactionIds[x], x.ToString())));

        return builder;
    }

    public static async Task ResolveMoveAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player, PlannedMove move)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var destinationHex = game.GetHexAt(move.Destination);
        if (destinationHex.Planet == null)
        {
            throw new Exception();
        }
        
        var moverName = await player.GetNameAsync(true);

        // Stage 1: Subtract moving forces from each source planet and calculate total forces moving
        var totalMoving = 0;
        foreach (var source in move.Sources)
        {
            var sourceHex = game.GetHexAt(source.Source);
            if (sourceHex.Planet == null
                || sourceHex.Planet.ForcesPresent < source.Amount
                || sourceHex.Planet.OwningPlayerId != player.GamePlayerId)
            {
                throw new Exception();
            }

            sourceHex.Planet.SubtractForces(source.Amount);
            totalMoving += source.Amount;
            builder.AppendContentNewline($"Moving {source.Amount} from {source.Source}");
        }

        if (move.Sources.Count > 1)
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

        builder.AppendContentNewline(
            totalPostCapacityLimit > 0
                ? $"{moverName} now has {player.PlayerColourInfo.GetDieEmoji(totalPostCapacityLimit)} present on {destinationHex.Coordinates}"
                : $"All forces destroy each other, leaving {destinationHex.Coordinates} unoccupied");
        
        await GameFlowOperations.CheckForPlayerEliminationsAsync(builder, game);
    }
}