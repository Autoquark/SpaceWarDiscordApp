using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.GameLogic.MapGeneration;

public class PaulbyMapGenerator : BaseMapGenerator
{
    public PaulbyMapGenerator() : base("space-flower", "Space Flower", CollectionExtensions.Between(2, 6))
    {
    }

    public override List<BoardHex> GenerateMapInternal(Game game)
    {
        var map = new List<BoardHex>();

        var playerCount = game.Players.Count;

        // Centre
        var system = new BoardHex(CenterSystems.Random());
        system.Coordinates = new HexCoordinates(0, 0);
        map.Add(system);

        // Player slices
        var playerSliceRotations = playerCount switch
        {
            2 => new List<int> { 0, 3 },
            3 => new List<int> { 0, 2, 4 },
            _ => new List<int> { 0, 1, 3, 4 }
        };
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
            system.Coordinates = new HexCoordinates(1, -3).RotateClockwise(rotation);
            system.Planet!.OwningPlayerId = player.GamePlayerId;
            map.Add(system);

            // Neighbours
            OuterSystems.Shuffle();
            system = new BoardHex(OuterSystems[0]);
            system.Coordinates = new HexCoordinates(2, -3).RotateClockwise(rotation);
            map.Add(system);

            system = new BoardHex(OuterSystems[1]);
            system.Coordinates = new HexCoordinates(0, -2).RotateClockwise(rotation);
            map.Add(system);

            system = new BoardHex(OuterSystems[2]);
            system.Coordinates = new HexCoordinates(1, -2).RotateClockwise(rotation);
            map.Add(system);

            // Inner system
            system = new BoardHex(InnerSystems.Random());
            system.Coordinates = new HexCoordinates(0, -1).RotateClockwise(rotation);
            map.Add(system);
        }

        // Empty slices
        foreach (var rotation in Enumerable.Range(0, 6).Except(playerSliceRotations))
        {
            // Outer hyperlane
            /*system = new BoardHex()
            {
                Coordinates = new HexCoordinates(-1, -2).RotateClockwise(rotation),
                HyperlaneConnections =
                [
                    new HyperlaneConnection(HexDirection.SouthWest.RotateClockwise(rotation),
                        HexDirection.NorthEast.RotateClockwise(rotation))
                ]
            };
            map.Add(system);*/

            List<HexDirection> connections = [HexDirection.SouthEast, HexDirection.SouthWest];
            if (playerSliceRotations.Contains(MathEx.Modulo(rotation - 1, 6)))
            {
                connections.Add(HexDirection.NorthWest);
            }
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(0, -2).RotateClockwise(rotation),
                HyperlaneConnections = connections
                    .Combinations()
                    .Select(x => new HyperlaneConnection(x.first.RotateClockwise(rotation), x.second.RotateClockwise(rotation)))
                    .ToList()
            };
            map.Add(system);
            
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(1, -2).RotateClockwise(rotation),
                HyperlaneConnections = [new HyperlaneConnection(HexDirection.NorthWest.RotateClockwise(rotation), HexDirection.SouthEast.RotateClockwise(rotation))]
            };
            map.Add(system);

            // Inner hyperlane
            connections = [HexDirection.SouthWest, HexDirection.SouthEast];
            if (playerSliceRotations.Contains(MathEx.Modulo(rotation - 1, 6)))
            {
                connections.Add(HexDirection.NorthWest);
            }
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(0, -1).RotateClockwise(rotation),
                HyperlaneConnections = connections
                    .Combinations()
                    .Select(x => new HyperlaneConnection(x.first.RotateClockwise(rotation), x.second.RotateClockwise(rotation)))
                    .ToList()

            };
            map.Add(system);
        }

        return map;
    }
}