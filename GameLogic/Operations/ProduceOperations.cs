using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.EventRecords;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Discord;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public class ProduceOperations : IEventResolvedHandler<GameEvent_BeginProduce>, IEventResolvedHandler<GameEvent_PostProduce>
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
        player.Science += gameEvent.EffectiveScienceProduction;
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

        await GameFlowOperations.PushGameEventsAsync(builder, game, serviceProvider, new GameEvent_PostProduce
        {
            PlayerGameId = player.GamePlayerId,
            ForcesProduced = hex.Planet.Production,
            ScienceProduced = hex.Planet.Science,
            Location = hex.Coordinates
        });

        return builder;
    }
    
    public async Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent_PostProduce gameEvent, Game game,
        IServiceProvider serviceProvider)
    {
        var hex = game.GetHexAt(gameEvent.Location);
        var player = game.GetGamePlayerByGameId(gameEvent.PlayerGameId);
        var name = await player.GetNameAsync(false);
        
        CheckPlanetCapacity(builder, hex);

        if (gameEvent.ScienceProduced <= 0)
        {
            return builder;
        }
        
        builder?.AppendContentNewline($"{name} now has {player.Science} science");
        if (builder != null)
        {
            await TechOperations.ShowTechPurchaseButtonsAsync(builder, game, player, serviceProvider);
        }

        return builder;
    }

    public static DiscordMultiMessageBuilder? CheckPlanetCapacity(DiscordMultiMessageBuilder? builder, BoardHex hex)
    {
        if (hex.ForcesPresent > GameConstants.MaxForcesPerPlanet)
        {
            var loss = hex.ForcesPresent - GameConstants.MaxForcesPerPlanet;
            hex.Planet!.SetForces(GameConstants.MaxForcesPerPlanet);
            builder?.AppendContentNewline($"{loss} forces sadly had to be jettisoned into space from {hex.Coordinates} due to exceeding the capacity limit");
        }

        return builder;
    }
}