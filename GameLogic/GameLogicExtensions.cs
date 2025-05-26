using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.GameLogic;

public static class GameLogicExtensions
{
    public static IEnumerable<BoardHex> WhereOwnedBy(this IEnumerable<BoardHex> hexes, GamePlayer player)
        => hexes.Where(x => x.Planet?.OwningPlayerId == player.GamePlayerId);
}