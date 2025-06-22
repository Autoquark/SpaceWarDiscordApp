using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_SabotageSupplyLines : Tech
{
    public Tech_SabotageSupplyLines() : base("sabotageSupplyLines",
        "Sabotage Supply Lines",
        "Free Action, Once per turn: Remove 1 forces from each opponent's planet where there are 5 or more forces.",
        "Pleased to report the capture of an enemy Heinz-class intergalactic tomato soup tanker.")
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
        SimpleActionIsOncePerTurn = true;
    }

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player)
        => base.IsSimpleActionAvailable(game, player) && GetTargets(game, player).Any();

    public override async Task<TBuilder> UseTechActionAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player)
    {
        var targets = GetTargets(game, player).ToList();
        var tech = GetThisTech(player);

        foreach (var boardHex in targets)
        {
            boardHex.Planet!.SubtractForces(1);
        }

        var name = await player.GetNameAsync(false);
        builder.AppendContentNewline($"{name} used {DisplayName} to remove 1 forces from: " + string.Join(", ", targets.Select(x => x.ToCoordsWithDieEmoji(game))));
        
        tech.UsedThisTurn = true;

        return builder;
    }

    private static IEnumerable<BoardHex> GetTargets(Game game, GamePlayer player)
        => game.Hexes.WhereNotOwnedBy(player).Where(x => x.ForcesPresent >= 5);
}