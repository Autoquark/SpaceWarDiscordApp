using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_LocalDefenseForces : Tech
{
    public Tech_LocalDefenseForces() : base("localDefenseForces",
        "Local Defense Forces",
        "Add 1 forces to each of your planets where you have exactly 1 forces. Don't exhaust those planets.",
        "We pay taxes for the privilege of not getting bombed by our own government. Not getting bombed by the enemy is our own responsibility.",
        ["Free Action", "Exhaust"])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
    }

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player)
        => base.IsSimpleActionAvailable(game, player) && GetAffectedHexes(game, player).Any();

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var name = await player.GetNameAsync(false);
        var affectedHexes = GetAffectedHexes(game, player).ToList();
        builder.AppendContentNewline($"{name} is using {DisplayName}. Adding 1 forces to:");
        builder.AppendContentNewline(string.Join(", ", affectedHexes.Select(x => x.Coordinates)));
        
        foreach (var affectedHex in affectedHexes)
        {
            affectedHex.Planet!.ForcesPresent++;
            // Don't see how we actually could exceed capacity, but just in case of future complexity
            ProduceOperations.CheckPlanetCapacity(builder, affectedHex);
        }
        
        player.GetPlayerTechById(Id).IsExhausted = true;
        
        return (await GameFlowOperations.OnActionCompletedAsync(builder, game, SimpleActionType, serviceProvider))!;
    }
    
    private static IEnumerable<BoardHex> GetAffectedHexes(Game game, GamePlayer player)
        => game.Hexes.WhereOwnedBy(player).Where(x => x.ForcesPresent == 1);
}