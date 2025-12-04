using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_MassRecruitment : Tech
{
    public Tech_MassRecruitment() : base("massRecruitment",
        "Mass Recruitment",
        "Produce 1 forces on each of your ready planets, then exhaust those planets.",
        "We are expanding conscription to include the elderly, children over the age of 12 and most family pets.",
        [TechKeyword.Action])
    {
        HasSimpleAction = true;
    }

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var targets = game.Hexes.Where(x => x.Planet?.OwningPlayerId == player.GamePlayerId && !x.Planet.IsExhausted)
            .ToList();

        foreach (var boardHex in targets)
        {
            boardHex.Planet!.AddForces(1);
            boardHex.Planet.IsExhausted = true;
            ProduceOperations.CheckPlanetCapacity(game, boardHex);
        }
        
        builder.AppendContentNewline($"Used {DisplayName} to produce 1 forces on: " + string.Join(", ", targets.Select(x => x.Coordinates)));
        
        player.GetPlayerTechById(Id).IsExhausted = true;
        
        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            new GameEvent_ActionComplete
            {
                ActionType = SimpleActionType,
            });

        return builder;
    }
}