using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public static class RefreshOperations
{
    public static async Task<TBuilder> Refresh<TBuilder>(TBuilder builder, Game game, GamePlayer player)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var name = await player.GetNameAsync(false);
        builder.AppendContentNewline($"{name} is refreshing");

        var refreshedHexes = new HashSet<BoardHex>();
        foreach (var hex in game.Hexes.Where(x => x.Planet?.OwningPlayerId == player.GamePlayerId))
        {
            if (hex.Planet?.IsExhausted == true)
            {
                hex.Planet.IsExhausted = false;
                refreshedHexes.Add(hex);
            }
        }

        var refreshedTechs = new List<PlayerTech>();
        foreach (var playerTech in player.Techs)
        {
            if (playerTech.IsExhausted)
            {
                playerTech.IsExhausted = false;
                refreshedTechs.Add(playerTech);
            }
        }

        if (refreshedHexes.Count > 0)
        {
            builder.AppendContentNewline("Refreshed planets: " + string.Join(", ", refreshedHexes.Select(x => x.Coordinates)));
        }

        if (refreshedTechs.Count > 0)
        {
            builder.AppendContentNewline("Refreshed techs: " +
                                         string.Join(", ",
                                             refreshedTechs.Select(x => Tech.TechsById[x.TechId].DisplayName)));
        }
        
        if(refreshedTechs.Count + refreshedHexes.Count == 0)
        {
            builder.AppendContentNewline("Nothing to refresh!");
        }

        return builder;
    }
}