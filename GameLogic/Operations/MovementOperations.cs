using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.EventRecords;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Discord;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public class MovementOperations : IEventResolvedHandler<GameEvent_PreMove>
{
    /// <summary>
    /// Display a summary of the given player's current planned move
    /// </summary>
    public static async Task<DiscordMultiMessageBuilder> ShowPlannedMoveAsync(DiscordMultiMessageBuilder builder, GamePlayer player)
    {
        var plannedMove = player.PlannedMove;
        if (plannedMove == null)
        {
            throw new Exception();
        }
        
        var name = await player.GetNameAsync(false);
        builder.AppendContentNewline($"{name} is moving to {plannedMove.Destination}");
        
        foreach (var source in plannedMove.Sources)
        {
            builder.AppendContentNewline($"{source.Amount} from {source.Source}");
        }
        return builder;
    }

    public static async Task<IEnumerable<GameEvent>> GetResolveMoveEventsAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        PlannedMove move, IServiceProvider serviceProvider, string moveName)
    {
        var destinationHex = game.GetHexAt(move.Destination);
        if (destinationHex.Planet == null)
        {
            throw new Exception();
        }

        return [new GameEvent_PreMove
            {
                MovingPlayerId = player.GamePlayerId,
                Sources = move.Sources.ToList(),
                Destination = move.Destination,
                MoveName = moveName 
            }];
    }

    public async Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent_PreMove gameEvent, Game game,
        IServiceProvider serviceProvider)
    {
        var movingPlayer = game.GetGamePlayerByGameId(gameEvent.MovingPlayerId);
        var moverName = await movingPlayer.GetNameAsync(true);
        
        // Stage 1: Subtract moving forces from each source planet and calculate total forces moving
        builder?.AppendContentNewline($"{moverName} is moving to {gameEvent.Destination} ({gameEvent.MoveName})");
        var totalMoving = 0;
        foreach (var source in gameEvent.Sources)
        {
            var sourceHex = game.GetHexAt(source.Source);
            if (sourceHex.Planet == null
                || sourceHex.ForcesPresent < source.Amount
                || sourceHex.Planet.OwningPlayerId != movingPlayer.GamePlayerId)
            {
                throw new Exception();
            }

            sourceHex.Planet.SubtractForces(source.Amount);
            totalMoving += source.Amount;
            builder?.AppendContentNewline($"Moving {source.Amount} from {source.Source}");
        }

        if (gameEvent.Sources.Count > 1)
        {
            builder?.AppendContentNewline($"Moving a total of {totalMoving} forces");
        }
        
        var destinationHex = game.GetHexAt(gameEvent.Destination);
        if (destinationHex.Planet == null)
        {
            throw new Exception();
        }

        // Stage 2: Resolve combat or merging with allied forces
        var totalPreCapacityLimit = totalMoving;
        if (destinationHex.Planet.OwningPlayerId == movingPlayer.GamePlayerId || destinationHex.IsNeutral)
        {
            totalPreCapacityLimit += destinationHex.ForcesPresent;
        }
        else
        {
            // Combat
            var attackerCombatStrength = gameEvent.AttackerCombatStrengthSources.Sum(x => x.Amount);
            var defenderCombatStrength = gameEvent.DefenderCombatStrengthSources.Sum(x => x.Amount);
            
            var defender = game.GetGamePlayerByGameId(destinationHex.Planet.OwningPlayerId);
            var defenderName = await defender.GetNameAsync(true);

            if (attackerCombatStrength > 0)
            {
                builder?.AppendContentNewline($"{moverName} Combat Strength: {attackerCombatStrength} ({string.Join(", ", gameEvent.AttackerCombatStrengthSources)})");
            }

            if (defenderCombatStrength > 0)
            {
                builder?.AppendContentNewline($"{defenderName} Combat Strength: {defenderCombatStrength} ({string.Join(", ", gameEvent.DefenderCombatStrengthSources)})");
            }

            // Apply combat strength effects
            if (attackerCombatStrength > defenderCombatStrength)
            {
                var difference = attackerCombatStrength - defenderCombatStrength;
                destinationHex.Planet.SubtractForces(difference);
                builder?.AppendContentNewline($"{defenderName} loses {difference} forces before combat due to {moverName}'s superior Combat Strength");
            }
            else if (defenderCombatStrength > attackerCombatStrength)
            {
                var difference = defenderCombatStrength - attackerCombatStrength;
                totalPreCapacityLimit -= difference;
                builder?.AppendContentNewline($"{moverName} loses {difference} forces before combat due to {defenderName}'s superior Combat Strength");
            }
            
            var combatLoss = Math.Min(totalPreCapacityLimit, destinationHex.ForcesPresent);
            totalPreCapacityLimit -= combatLoss;
            destinationHex.Planet.SubtractForces(combatLoss);

            builder?.AppendContentNewline($"{moverName} and {defenderName} each lose {combatLoss} forces in combat")
                .WithAllowedMentions(movingPlayer, defender);
        }

        // Stage 3: Apply planet capacity limit
        var totalPostCapacityLimit = Math.Min(GameConstants.MaxForcesPerPlanet, totalPreCapacityLimit);
        var lossToCapacityLimit = Math.Max(0, totalPreCapacityLimit - totalPostCapacityLimit);

        if (lossToCapacityLimit > 0)
        {
            builder?.AppendContentNewline($"{moverName} lost {lossToCapacityLimit} forces that were exceeding the planet capacity");
        }
        
        // Stage 4: Save back to game state
        if (totalPostCapacityLimit > 0)
        {
            destinationHex.Planet.SetForces(totalPostCapacityLimit, movingPlayer.GamePlayerId);
        }

        if (movingPlayer == game.CurrentTurnPlayer)
        {
            movingPlayer.CurrentTurnEvents.Add(new MovementEventRecord
            {
                Destination = gameEvent.Destination,
                Sources = gameEvent.Sources.ToList()
            });
        }

        movingPlayer.PlannedMove = null;

        var newOwner = game.TryGetGamePlayerByGameId(destinationHex.Planet.OwningPlayerId);

        builder?.AppendContentNewline(
            newOwner != null
                ? $"{await newOwner.GetNameAsync(true)} now has {newOwner.PlayerColourInfo.GetDieEmoji(destinationHex.Planet.ForcesPresent)} present on {destinationHex.Coordinates}"
                : $"All forces destroy each other, leaving {destinationHex.Coordinates} unoccupied");
        
        return await GameFlowOperations.CheckForPlayerEliminationsAsync(builder, game);
    }
}