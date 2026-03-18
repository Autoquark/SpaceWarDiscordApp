using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_ScientificSuperiority : Tech
{
    public Tech_ScientificSuperiority() : base("scientificSuperiority", "Scientific Superiority",
        "If you control planets with more $science$ symbols than any other player, gain 1VP",
        "Our indomitable spirit of curiosity is WAY better than yours!",
        [TechKeyword.Action, TechKeyword.Exhaust])
    {
        HasSimpleAction = true;
    }

    public override bool ShouldIncludeInGame(Game game) => base.ShouldIncludeInGame(game) && game.Rules.ScoringRule != ScoringRule.Cumulative;

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player) => base.IsSimpleActionAvailable(game, player)
                                                                                     && GameStateOperations.GetPlayerScienceIconsControlled(game, player) > game.Players.Except(player).Max(x => GameStateOperations.GetPlayerScienceIconsControlled(game, x));

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var name = await player.GetNameAsync(false);
        builder.AppendContentNewline($"{name} gains 1 VP via Scientific Superiority!");

        player.VictoryPoints++;
        player.GetPlayerTechById(Id).IsExhausted = true;
        await GameFlowOperations.CheckForVictoryAsync(builder, game, serviceProvider);
        
        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            new GameEvent_ActionComplete
            {
                ActionType = SimpleActionType,
            });

        return builder;
    }
}