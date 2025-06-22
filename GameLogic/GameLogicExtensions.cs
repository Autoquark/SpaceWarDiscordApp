using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.GameLogic;

public static class GameLogicExtensions
{
    public static IEnumerable<BoardHex> WhereOwnedBy(this IEnumerable<BoardHex> hexes, GamePlayer player)
        => hexes.Where(x => x.Planet?.OwningPlayerId == player.GamePlayerId);
    
    public static IEnumerable<BoardHex> WhereNotOwnedBy(this IEnumerable<BoardHex> hexes, GamePlayer player)
        => hexes.Where(x => x.Planet?.OwningPlayerId != player.GamePlayerId);

    public static IEnumerable<BoardHex> WhereForcesPresent(this IEnumerable<BoardHex> hexes)
        => hexes.Where(x => x.AnyForcesPresent);
}