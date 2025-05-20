using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.DatabaseModels;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public class GameStateOperations
{
    public static int GetPlayerStars(Game game, GamePlayer player)
        => game.Hexes.Where(x => x.Planet?.OwningPlayerId == player.GamePlayerId)
            .Sum(x => x.Planet!.Stars);
}