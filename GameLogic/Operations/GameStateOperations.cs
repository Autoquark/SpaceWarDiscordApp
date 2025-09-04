using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public static class GameStateOperations
{
    public static int GetPlayerScienceIconsControlled(Game game, GamePlayer player)
        => game.Hexes.WhereOwnedBy(player).Sum(x => x.Planet!.Science);
    
    public static int GetPlayerStars(Game game, GamePlayer player)
        => game.Hexes.WhereOwnedBy(player).Sum(x => x.Planet!.Stars)
           // Hardcoded for now as there's only one tech that does this
           + (player.TryGetPlayerTechById(Tech_GlorificationMatrix.StaticId) == null ? 0 : 1);
}