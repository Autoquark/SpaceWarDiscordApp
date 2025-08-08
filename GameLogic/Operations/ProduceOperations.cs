using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.EventRecords;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Discord;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public class ProduceOperations : IEventResolvedHandler<GameEvent_BeginProduce>, IEventResolvedHandler<GameEvent_PostProduce>
{
    public static async Task<DiscordMultiMessageBuilder?> PushProduceOnPlanetAsync(DiscordMultiMessageBuilder? builder, Game game, BoardHex hex, IServiceProvider serviceProvider) 
    {
        if (hex.Planet == null)
        {
            throw new Exception();
        }
        
        var player = game.GetGamePlayerByGameId(hex.Planet.OwningPlayerId);
        var name = await player.GetNameAsync(false);
        
        hex.Planet.AddForces(hex.Planet.Production);
        hex.Planet.IsExhausted = true;
        player.Science += hex.Planet.Science;
        var producedScience = hex.Planet.Science > 0;

        if (game.CurrentTurnPlayer == player)
        {
            player.CurrentTurnEvents.Add(new ProduceEventRecord
            {
                Coordinates = hex.Coordinates
            });
        }

        builder?.AppendContentNewline(
            $"{name} is producing on {hex.Coordinates}. Produced {hex.Planet.Production} forces" + (producedScience ? $" and {hex.Planet.Science} science" : ""));

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
            hex.Planet!.ForcesPresent = GameConstants.MaxForcesPerPlanet;
            builder?.AppendContentNewline($"{loss} forces sadly had to be jettisoned into space from {hex.Coordinates} due to exceeding the capacity limit");
        }

        return builder;
    }

    public async Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent_BeginProduce gameEvent, Game game,
        IServiceProvider serviceProvider)
        => await PushProduceOnPlanetAsync(builder, game, game.GetHexAt(gameEvent.Location), serviceProvider);
}