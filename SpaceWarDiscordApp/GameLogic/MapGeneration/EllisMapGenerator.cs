using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.GameLogic.MapGeneration;

public class EllisMapGenerator : BaseMapGenerator
{
    public EllisMapGenerator() : base("ellis-1", "Mini Madness", [3, 6])
    {
    }

    public override List<BoardHex> GenerateMapInternal(Game game)
    {
        var playerCount = game.Players.Count;

        // Edit default tiles
        CenterSystems.Clear();
        CenterSystems.Add(new BoardHex() { Planet = new Planet() { Stars = 3, Science = (playerCount == 6) ? 3 : 2 } });
        InnerSystems.Clear();
        InnerSystems.Add(new BoardHex() { Planet = new Planet() { Stars = 2, Production = 4 } });
        InnerSystems.Add(new BoardHex() { Planet = new Planet() { Stars = 2, Production = 2, Science = 1 } });
        // Keep OuterSystems the same
        HomeSystems.Clear();
        HomeSystems.Add(new BoardHex() { Planet = new Planet() { Stars = 1, Production = 3, Science = 1, IsHomeSystem = true, ForcesPresent = 3, } });

        var map = new List<BoardHex>();


        // Centre
        var system = new BoardHex(CenterSystems.Random());
        system.Coordinates = new HexCoordinates(0, 0);
        map.Add(system);

        // Player slices for first 3 players
        for (int rotation = 0; rotation < 6; rotation += 2)
        {
            int playerIndex = playerCount == 6 ? rotation : rotation / 2;

            // Home system
            system = new BoardHex(HomeSystems.Random());
            system.Coordinates = new HexCoordinates(0, -2).RotateClockwise(rotation);
            system.Planet!.OwningPlayerId = game.Players[playerIndex].GamePlayerId;
            map.Add(system);

            // Neighbours
            OuterSystems.Shuffle();
            InnerSystems.Shuffle();
            system = new BoardHex(OuterSystems[0]);
            system.Coordinates = new HexCoordinates(-1, -1).RotateClockwise(rotation);
            map.Add(system);

            system = new BoardHex(playerCount == 6 ? InnerSystems[0] : OuterSystems[1]);
            system.Coordinates = new HexCoordinates(0, -1).RotateClockwise(rotation);
            map.Add(system);

            system = new BoardHex(OuterSystems[2]);
            system.Coordinates = new HexCoordinates(1, -2).RotateClockwise(rotation);
            map.Add(system);

            // Inner system
            system = new BoardHex(InnerSystems[1]);
            system.Coordinates = new HexCoordinates(1, -1).RotateClockwise(rotation);
            map.Add(system);

            if (playerCount == 3) // Hyperlanes for 3 player variation
            {
                system = new BoardHex()
                {
                    Coordinates = new HexCoordinates(2, -2).RotateClockwise(rotation),
                    HyperlaneConnections = [new HyperlaneConnection(HexDirection.NorthWest.RotateClockwise(rotation), HexDirection.South.RotateClockwise(rotation))]
                };
                map.Add(system);
            } else // Home systems for 6 player variation
            {
                system = new BoardHex(HomeSystems.Random());
                system.Coordinates = new HexCoordinates(2, -2).RotateClockwise(rotation);
                system.Planet!.OwningPlayerId = game.Players[playerIndex + 1].GamePlayerId;
                map.Add(system);
            }
        }

        return map;
    }
}