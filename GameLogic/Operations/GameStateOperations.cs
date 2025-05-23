using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public static class GameStateOperations
{
    public static int GetPlayerStars(Game game, GamePlayer player)
        => game.Hexes.Where(x => x.Planet?.OwningPlayerId == player.GamePlayerId)
            .Sum(x => x.Planet!.Stars);
}