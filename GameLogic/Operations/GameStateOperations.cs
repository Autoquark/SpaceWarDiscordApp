using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public static class GameStateOperations
{
    public static int GetPlayerScienceIconsControlled(Game game, GamePlayer player)
        => game.Hexes.WhereOwnedBy(player).Sum(x => x.Planet!.Science);
    
    public static int GetPlayerStars(Game game, GamePlayer player)
        => game.Hexes.WhereOwnedBy(player).Sum(x => x.Planet!.Stars);
}