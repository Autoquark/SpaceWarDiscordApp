using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_ScientificSuperiority : Tech
{
    public Tech_ScientificSuperiority() : base("scientificSuperiority", "Scientific Superiority",
        "Action, Exhaust: If you control planets with more $science$ symbols than any other player, gain 1VP",
        "Our indomitable spirit of curiosity is WAY better than yours!")
    {
        HasSimpleAction = true;
    }

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player) => base.IsSimpleActionAvailable(game, player)
        && GameStateOperations.GetPlayerScienceIconsControlled(game, player) > game.Players.Except(player).Max(x => GameStateOperations.GetPlayerScienceIconsControlled(game, x));

    public override async Task<TBuilder> UseTechActionAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player)
    {
        var name = await player.GetNameAsync(false);
        builder.AppendContentNewline($"{name} gains 1 VP via Scientific Superiority!");

        player.VictoryPoints++;
        player.GetPlayerTechById(Id).IsExhausted = true;
        await GameFlowOperations.CheckForVictoryAsync(builder, game);
        await GameFlowOperations.OnActionCompleted(builder, game, ActionType.Main);

        return builder;
    }
}