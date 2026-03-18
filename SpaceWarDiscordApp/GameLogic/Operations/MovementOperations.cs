using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.EventRecords;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Movement;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public class MovementOperations : IEventResolvedHandler<GameEvent_PreMove>, IEventResolvedHandler<GameEvent_CapturePlanet>
{
    public const string DefaultMoveName = "Move Action";
    
    /// <summary>
    /// Display a summary of the given player's current planned move
    /// </summary>
    public static async Task<DiscordMultiMessageBuilder> ShowPlannedMoveAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player)
    {
        var plannedMove = player.PlannedMove;
        if (plannedMove == null)
        {
            throw new Exception();
        }
        
        var destination = game.GetHexAt(plannedMove.Destination);
        var name = await player.GetNameAsync(false);
        builder.AppendContentNewline($"{name} is moving to {destination.ToHexNumberWithDieEmoji(game)}");
        
        foreach (var source in plannedMove.Sources)
        {
            builder.AppendContentNewline($"{source.Amount} from {source.Source}");
        }
        return builder;
    }

    public static async Task<IEnumerable<GameEvent>> GetResolveMoveEventsAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        PlannedMove move, IServiceProvider serviceProvider, Tech? tech)
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
                TechId = tech?.Id
            }];
    }

    public async Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent_PreMove gameEvent, Game game,
        IServiceProvider serviceProvider)
    {
        var movingPlayer = game.GetGamePlayerByGameId(gameEvent.MovingPlayerId);
        var moverName = await movingPlayer.GetNameAsync(true);
        
        // Stage 1: Subtract moving forces from each source planet and calculate total forces moving
        var moveName = gameEvent.TechId != null ? Tech.TechsById[gameEvent.TechId].DisplayName : DefaultMoveName;
        builder?.AppendContentNewline($"{moverName} is moving to {gameEvent.Destination} ({moveName})");
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
        var oldOwner = destinationHex.Planet.OwningPlayerId; 
        if (oldOwner == movingPlayer.GamePlayerId || destinationHex.IsNeutral)
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
            var attackerCombatStrengthLoss = 0;
            var defenderCombatStrengthLoss = 0;
            if (attackerCombatStrength > defenderCombatStrength)
            {
                var difference = attackerCombatStrength - defenderCombatStrength;
                defenderCombatStrengthLoss = Math.Min(difference, destinationHex.Planet.ForcesPresent);
                builder?.AppendContentNewline($"{defenderName} loses {difference} forces before combat due to {moverName}'s superior Combat Strength");
            }
            else if (defenderCombatStrength > attackerCombatStrength)
            {
                var difference = defenderCombatStrength - attackerCombatStrength;
                attackerCombatStrengthLoss = Math.Min(difference, totalPreCapacityLimit);
                totalPreCapacityLimit -= attackerCombatStrengthLoss;
                builder?.AppendContentNewline($"{moverName} loses {difference} forces before combat due to {defenderName}'s superior Combat Strength");
            }
            
            var combatLoss = Math.Min(totalPreCapacityLimit, destinationHex.ForcesPresent - defenderCombatStrengthLoss);
            totalPreCapacityLimit -= combatLoss;
            
            // Can't call DestroyForces for attackers as that would attempt to remove forces from the planet, which would
            // actually remove defending forces. We still report the destruction as occurring on the planet though 
            GameFlowOperations.PushGameEvents(game, new GameEvent_PostForcesDestroyed
            {
                Amount = combatLoss + attackerCombatStrengthLoss,
                Location = destinationHex.Coordinates,
                OwningPlayerGameId = movingPlayer.GamePlayerId,
                ResponsiblePlayerGameId = defender.GamePlayerId,
                Reason = ForcesDestructionReason.Combat
            });
            GameFlowOperations.DestroyForces(game, destinationHex, combatLoss + defenderCombatStrengthLoss, defender.GamePlayerId, ForcesDestructionReason.Combat);

            builder?.AppendContentNewline($"{moverName} and {defenderName} each lose {combatLoss} forces in combat")
                .WithAllowedMentions(movingPlayer, defender);
        }

        // After resolving combat, if the attacker has forces left, put their remaining forces onto the planet
        if (totalPreCapacityLimit > 0)
        {
            destinationHex.Planet.SetForces(totalPreCapacityLimit, movingPlayer.GamePlayerId);
        }

        // Stage 3: If exceeding capacity, queue event to remove excess forces
        ProduceOperations.CheckPlanetCapacity(game, destinationHex);

        if (movingPlayer == game.CurrentTurnPlayer)
        {
            movingPlayer.CurrentTurnEvents.Add(new MovementEventRecord
            {
                Destination = gameEvent.Destination,
                Sources = gameEvent.Sources.ToList(),
                IsTechMove = gameEvent.TechId != null
            });
        }

        movingPlayer.PlannedMove = null;

        var newOwner = game.TryGetGamePlayerByGameId(destinationHex.Planet.OwningPlayerId);

        builder?.AppendContentNewline(
            newOwner != null
                ? $"{await newOwner.GetNameAsync(true)} now has {newOwner.PlayerColourInfo.GetDieEmojiOrNumber(destinationHex.Planet.ForcesPresent)} present on {destinationHex.Coordinates}"
                : $"All forces destroy each other, leaving {destinationHex.Coordinates} unoccupied");

        if (newOwner != null && newOwner.GamePlayerId != oldOwner)
        {
            GameFlowOperations.PushGameEvents(game, new GameEvent_CapturePlanet
            {
                FormerOwnerGameId = oldOwner,
                Location = destinationHex.Coordinates
            });
        }
        return await GameFlowOperations.CheckForPlayerEliminationsAsync(builder, game);
    }

    public Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent_CapturePlanet gameEvent, Game game,
        IServiceProvider serviceProvider)
    {
        // Doesn't need to do anything, this is just for techs to hook into
        return Task.FromResult(builder);
    }
}