using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.EventRecords;
using SpaceWarDiscordApp.Discord;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public static class ProduceOperations
{
    public static async Task<TBuilder> ProduceOnPlanetAsync<TBuilder>(TBuilder builder, Game game, BoardHex hex) 
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        if (hex.Planet == null)
        {
            throw new Exception();
        }
        
        var player = game.GetGamePlayerByGameId(hex.Planet.OwningPlayerId);
        var name = await player.GetNameAsync(false);
        
        hex.Planet.ForcesPresent += hex.Planet.Production;
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

        builder.AppendContentNewline(
            $"{name} is producing on {hex.Coordinates}. Produced {hex.Planet.Production} forces" + (producedScience ? $" and {hex.Planet.Science} science" : ""));
        CheckPlanetCapacity(builder, hex);
        
        if (producedScience)
        {
            builder.AppendContentNewline($"{name} now has {player.Science} science");
            await TechOperations.ShowTechPurchaseButtonsAsync(builder, game, player);
        }

        return builder;
    }

    public static TBuilder CheckPlanetCapacity<TBuilder>(TBuilder builder, BoardHex hex)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        if (hex.ForcesPresent > GameConstants.MaxForcesPerPlanet)
        {
            var loss = hex.ForcesPresent - GameConstants.MaxForcesPerPlanet;
            hex.Planet!.ForcesPresent = GameConstants.MaxForcesPerPlanet;
            builder.AppendContentNewline($"{loss} forces sadly had to be jettisoned into space from {hex.Coordinates} due to exceeding the capacity limit");
        }

        return builder;
    }
}