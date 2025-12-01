using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.EventRecords;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_SabotageSupplyLines : Tech
{
    public Tech_SabotageSupplyLines() : base("sabotageSupplyLines",
        "Sabotage Supply Lines",
        "Remove 1 forces from each opponent's planet where there are 5 or more forces.",
        "Pleased to report the capture of an enemy Heinz-class intergalactic tomato soup tanker.",
        [TechKeyword.FreeAction, TechKeyword.OncePerTurn])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
        SimpleActionIsOncePerTurn = true;
    }

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player)
        => base.IsSimpleActionAvailable(game, player) && GetTargets(game, player).Any();

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var targets = GetTargets(game, player).ToList();
        var tech = GetThisTech(player);

        foreach (var boardHex in targets)
        {
            GameFlowOperations.DestroyForces(game, boardHex, 1, player.GamePlayerId, ForcesDestructionReason.Tech);
            player.CurrentTurnEvents.Add(new PlanetTargetedTechEventRecord
            {
                Coordinates = boardHex.Coordinates
            });
        }

        var name = await player.GetNameAsync(false);
        builder.AppendContentNewline($"{name} used {DisplayName} to remove 1 forces from: " + string.Join(", ", targets.Select(x => x.ToHexNumberWithDieEmoji(game))));
        
        tech.UsedThisTurn = true;

        await GameFlowOperations.ContinueResolvingEventStackAsync(builder, game, serviceProvider);

        return builder;
    }

    private static IEnumerable<BoardHex> GetTargets(Game game, GamePlayer player)
        => game.Hexes.WhereNotOwnedBy(player).Where(x => x.ForcesPresent >= 5);
}