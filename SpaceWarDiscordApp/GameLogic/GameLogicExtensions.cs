using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.GameLogic;

public static class GameLogicExtensions
{
    public static IEnumerable<BoardHex> WhereOwnedBy(this IEnumerable<BoardHex> hexes, int playerId)
        => hexes.Where(x => x.Planet?.OwningPlayerId == playerId);
    public static IEnumerable<BoardHex> WhereOwnedBy(this IEnumerable<BoardHex> hexes, GamePlayer player)
        => hexes.WhereOwnedBy(player.GamePlayerId);
    
    public static IEnumerable<BoardHex> WhereNotOwnedBy(this IEnumerable<BoardHex> hexes, GamePlayer player)
        => hexes.WhereNotOwnedBy(player.GamePlayerId);
    
    public static IEnumerable<BoardHex> WhereNotOwnedBy(this IEnumerable<BoardHex> hexes, int playerId)
        => hexes.Where(x => x.Planet?.OwningPlayerId != playerId);

    public static IEnumerable<BoardHex> WhereForcesPresent(this IEnumerable<BoardHex> hexes)
        => hexes.Where(x => x.AnyForcesPresent);
}