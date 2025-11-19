using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.EventRecords;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Discord;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public class ProduceOperations : IEventResolvedHandler<GameEvent_BeginProduce>,
    IEventResolvedHandler<GameEvent_PostProduce>,
    IEventResolvedHandler<GameEvent_ExceedingPlanetCapacity>
{
    public static GameEvent_BeginProduce CreateProduceEvent(Game game, HexCoordinates location, bool allowExhausted = false)
    {
        var hex = game.GetHexAt(location);
        if (hex.Planet == null || hex.Planet.IsExhausted && !allowExhausted)
        {
            throw new Exception();
        }

        return new GameEvent_BeginProduce
        {
            Location = location,
            EffectiveProductionValue = hex.Planet.Production,
            EffectiveScienceProduction = hex.Planet.Science
        };
    }

    public async Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent_BeginProduce gameEvent, Game game,
        IServiceProvider serviceProvider)
    {
        var hex = game.GetHexAt(gameEvent.Location);
        if (hex.Planet == null)
        {
            throw new Exception();
        }
        
        var player = game.GetGamePlayerByGameId(hex.Planet.OwningPlayerId);
        var name = await player.GetNameAsync(false);
        
        hex.Planet.AddForces(gameEvent.EffectiveProductionValue);
        hex.Planet.IsExhausted = true;
        var producedScience = gameEvent.EffectiveScienceProduction > 0;

        if (game.CurrentTurnPlayer == player)
        {
            player.CurrentTurnEvents.Add(new ProduceEventRecord
            {
                Coordinates = hex.Coordinates
            });
        }

        builder?.AppendContentNewline(
            $"{name} is producing on {hex.Coordinates}. Produced {gameEvent.EffectiveProductionValue} forces" + (producedScience ? $" and {gameEvent.EffectiveScienceProduction} science" : ""));

        GameFlowOperations.PushGameEvents(game, new GameEvent_PlayerGainScience
            {
                PlayerGameId = player.GamePlayerId,
                Amount = gameEvent.EffectiveScienceProduction
            },
            new GameEvent_PostProduce
            {
                PlayerGameId = player.GamePlayerId,
                ForcesProduced = gameEvent.EffectiveProductionValue,
                ScienceProduced = hex.Planet.Science,
                Location = hex.Coordinates
            });

        return builder;
    }

    public async Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder,
        GameEvent_PostProduce gameEvent, Game game,
        IServiceProvider serviceProvider)
    {
        var hex = game.GetHexAt(gameEvent.Location);

        CheckPlanetCapacity(game, hex);
        
        return builder;
    }

    public static void CheckPlanetCapacity(Game game, BoardHex hex)
    {
        if (hex.ForcesPresent > GameConstants.MaxForcesPerPlanet)
        {
            GameFlowOperations.PushGameEvents(game, CreateExceedingPlanetCapacityEvent(hex));
        }
    }

    private static GameEvent_ExceedingPlanetCapacity CreateExceedingPlanetCapacityEvent(BoardHex hex) =>
        new()
        {
            Location = hex.Coordinates,
            Capacity = GameConstants.MaxForcesPerPlanet
        };

    public Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent_ExceedingPlanetCapacity gameEvent,
        Game game, IServiceProvider serviceProvider)
    {
        var hex = game.GetHexAt(gameEvent.Location);
        if (hex.ForcesPresent > gameEvent.Capacity)
        {
            var loss = hex.ForcesPresent - gameEvent.Capacity;
            hex.Planet!.SetForces(gameEvent.Capacity);
            builder?.AppendContentNewline($"{loss} forces sadly had to be jettisoned into space from {hex.Coordinates} due to exceeding the capacity limit");
        }
        
        return Task.FromResult(builder);
    }
}