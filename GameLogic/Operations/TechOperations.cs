using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public class TechOperations
{
    public static async Task<TBuilder> PurchaseTechAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player,
        string techId, int cost)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var name = await player.GetNameAsync(false);
        var tech = Tech.TechsById[techId];

        if (player.Science < cost)
        {
            throw new Exception();
        }

        var originalScience = player.Science;
        player.Science -= cost;
        player.Techs.Add(tech.CreatePlayerTech(game, player));
        
        builder.AppendContentNewline($"{name} purchases {tech.DisplayName} for {cost} Science ({originalScience} -> {player.Science})");
        
        return builder;
    }
}