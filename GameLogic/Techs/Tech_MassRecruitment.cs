using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_MassRecruitment : Tech
{
    public Tech_MassRecruitment() : base("massRecruitment",
        "Mass Recruitment",
        "Action: Produce 1 forces on each of your ready planets, then exhaust those planets.",
        "We are expanding conscription to include the elderly, children over the age of 12 and most family pets.")
    {
        HasSimpleAction = true;
    }

    public override async Task<TBuilder> UseTechActionAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player)
    {
        var targets = game.Hexes.Where(x => x.Planet?.OwningPlayerId == player.GamePlayerId && !x.Planet.IsExhausted)
            .ToList();

        foreach (var boardHex in targets)
        {
            boardHex.Planet!.ForcesPresent++;
            ProduceOperations.CheckPlanetCapacity(builder, boardHex);
        }
        
        builder.AppendContentNewline($"Used {DisplayName} to produce 1 forces on: " + string.Join(", ", targets.Select(x => x.Coordinates)));
        
        player.GetPlayerTechById(Id).IsExhausted = true;
        
        await GameFlowOperations.OnActionCompleted(builder, game, ActionType.Main);

        return builder;
    }
}