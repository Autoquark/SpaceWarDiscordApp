using System.Text;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_Bioweapon : Tech
{
    public Tech_Bioweapon() : base("bioweapon", "Bioweapon",
        "Remove 1 forces from each of your opponent's planets.",
        "Quick, sneeze on this missile.",
        ["Single Use", "Action"])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Main;
    }

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var name = await player.GetNameAsync(false);
        builder.AppendContentNewline($"{name} is unleashing a deadly bioweapon!");
        var stringBuilder = new StringBuilder("Removed 1 forces from: ");
        
        var affected = game.Hexes.WhereForcesPresent().WhereNotOwnedBy(player).ToList();
        stringBuilder.AppendJoin(", ", affected.Select(x => x.ToCoordsWithDieEmoji(game)));
        
        foreach (var hex in affected)
        {
            hex.Planet!.SubtractForces(1);
        }
        
        builder.AppendContentNewline(stringBuilder.ToString());
        
        player.Techs.Remove(GetThisTech(player));
        
        await GameFlowOperations.CheckForPlayerEliminationsAsync(builder, game);
        
        await GameFlowOperations.OnActionCompletedAsync(builder, game, ActionType.Main, serviceProvider);
        return builder;
    }
}