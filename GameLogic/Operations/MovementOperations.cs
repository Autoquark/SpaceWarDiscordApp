using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
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
                || sourceHex.ForcesPresent < source.Amount
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
        if (destinationHex.Planet.OwningPlayerId == player.GamePlayerId || destinationHex.IsNeutral)
        {
            totalPreCapacityLimit += destinationHex.ForcesPresent;
        }
        else
        {
            var defenderName =
                await game.GetGamePlayerByGameId(destinationHex.Planet.OwningPlayerId).GetNameAsync(true);
            var combatLoss = Math.Min(totalMoving, destinationHex.ForcesPresent);
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