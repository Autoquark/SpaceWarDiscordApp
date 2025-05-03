using SpaceWarDiscordApp.DatabaseModels;

namespace SpaceWarDiscordApp.GameLogic;

public static class HexCoordinateExtensions
{
    public static HexCoordinates ToHexOffset(this HexDirection direction)
    {
        return direction switch
        {
            HexDirection.North => new HexCoordinates(0, -1),
            HexDirection.NorthEast => new HexCoordinates(1, -1),
            HexDirection.SouthEast => new HexCoordinates(1, 0),
            HexDirection.South => new HexCoordinates(0, 1),
            HexDirection.SouthWest => new HexCoordinates(-1, 1),
            HexDirection.NorthWest => new HexCoordinates(-1, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }
}