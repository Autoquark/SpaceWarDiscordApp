using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.GameLogic.MapGeneration;

public class EllisMapGenerator : BaseMapGenerator
{
    public EllisMapGenerator() : base("ellis-1", "Ellis", CollectionExtensions.Between(3, 3))
    {
    }

    public override List<BoardHex> GenerateMapInternal(Game game)
    {
        // Edit default tiles
        CenterSystems.Clear();
        CenterSystems.Add(new BoardHex() { Planet = new Planet() { Stars = 3, Science = 2 } });
        InnerSystems.Clear();
        InnerSystems.Add(new BoardHex() { Planet = new Planet() { Stars = 2, Production = 4 } });
        InnerSystems.Add(new BoardHex() { Planet = new Planet() { Stars = 2, Production = 2, Science = 1 } });
        // Keep OuterSystems the same
        HomeSystems.Clear();
        HomeSystems.Add(new BoardHex() { Planet = new Planet() { Stars = 1, Production = 3, Science = 1, IsHomeSystem = true, ForcesPresent = 3, } });

        var map = new List<BoardHex>();

        var playerCount = game.Players.Count;

        // Centre
        var system = new BoardHex(CenterSystems.Random());
        system.Coordinates = new HexCoordinates(0, 0);
        map.Add(system);

        // Player slices
        var playerSliceRotations = playerCount == 3 ? new List<int> { 0, 2, 4 } : new List<int> { 0, 1, 3, 4 };
        if (playerCount >= 5)
        {
            playerSliceRotations.Add(2);
        }

        if (playerCount >= 6)
        {
            playerSliceRotations.Add(5);
        }

        foreach (var (rotation, player) in playerSliceRotations.Zip(game.Players))
        {
            // Home system
            system = new BoardHex(HomeSystems.Random());
            system.Coordinates = new HexCoordinates(0, -2).RotateClockwise(rotation);
            system.Planet!.OwningPlayerId = player.GamePlayerId;
            map.Add(system);

            // Neighbours
            OuterSystems.Shuffle();
            system = new BoardHex(OuterSystems[0]);
            system.Coordinates = new HexCoordinates(-1, -1).RotateClockwise(rotation);
            map.Add(system);

            system = new BoardHex(OuterSystems[1]);
            system.Coordinates = new HexCoordinates(0, -1).RotateClockwise(rotation);
            map.Add(system);

            system = new BoardHex(OuterSystems[2]);
            system.Coordinates = new HexCoordinates(1, -2).RotateClockwise(rotation);
            map.Add(system);

            // Inner system
            system = new BoardHex(InnerSystems.Random());
            system.Coordinates = new HexCoordinates(1, -1).RotateClockwise(rotation);
            map.Add(system);

            // Hyperlane
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(2, -2).RotateClockwise(rotation),
                HyperlaneConnections = [new HyperlaneConnection(HexDirection.NorthWest.RotateClockwise(rotation), HexDirection.South.RotateClockwise(rotation))]
            };
            map.Add(system);
        }

        return map;
    }
}