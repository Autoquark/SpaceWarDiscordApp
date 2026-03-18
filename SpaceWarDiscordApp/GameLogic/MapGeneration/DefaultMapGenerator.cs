using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.GameLogic.MapGeneration;

public class DefaultMapGenerator : BaseMapGenerator
{
    public static string StaticId => "default";
    
    public DefaultMapGenerator() : base(StaticId, "Default", CollectionExtensions.Between(2, 6))
    {
    }
    
    public override List<BoardHex> GenerateMapInternal(Game game)
    {
        var map = new List<BoardHex>();
        
        var playerCount = game.Players.Count;
        if (playerCount is >= 3 and <= 6)
        {
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
                system.Coordinates = new HexCoordinates(0, -3).RotateClockwise(rotation);
                system.Planet!.OwningPlayerId = player.GamePlayerId;
                map.Add(system);
            
                // Neighbours
                OuterSystems.Shuffle();
                system = new BoardHex(OuterSystems[0]);
                system.Coordinates = new HexCoordinates(-1, -2).RotateClockwise(rotation);
                map.Add(system);
            
                system = new BoardHex(OuterSystems[1]);
                system.Coordinates = new HexCoordinates(0, -2).RotateClockwise(rotation);
                map.Add(system);
            
                system = new BoardHex(OuterSystems[2]);
                system.Coordinates = new HexCoordinates(1, -3).RotateClockwise(rotation);
                map.Add(system);
                
                // Inner system
                system = new BoardHex(InnerSystems.Random());
                system.Coordinates = new HexCoordinates(0, -1).RotateClockwise(rotation);
                map.Add(system);
                
                // Hyperlane
                system = new BoardHex()
                {
                    Coordinates = new HexCoordinates(1, -2).RotateClockwise(rotation),
                    HyperlaneConnections = [new HyperlaneConnection(HexDirection.North.RotateClockwise(rotation), HexDirection.SouthWest.RotateClockwise(rotation))]
                };
                map.Add(system);
            }

            // Empty slices
            foreach (var rotation in Enumerable.Range(0, 6).Except(playerSliceRotations))
            {
                // Outer hyperlane
                system = new BoardHex()
                {
                    Coordinates = new HexCoordinates(-1, -2).RotateClockwise(rotation),
                    HyperlaneConnections = [new HyperlaneConnection(HexDirection.SouthWest.RotateClockwise(rotation), HexDirection.NorthEast.RotateClockwise(rotation))]
                };
                map.Add(system);
                
                system = new BoardHex()
                {
                    Coordinates = new HexCoordinates(0, -3).RotateClockwise(rotation),
                    HyperlaneConnections = [new HyperlaneConnection(HexDirection.SouthWest.RotateClockwise(rotation), HexDirection.SouthEast.RotateClockwise(rotation))]
                };
                map.Add(system);
                
                system = new BoardHex()
                {
                    Coordinates = new HexCoordinates(1, -3).RotateClockwise(rotation),
                    HyperlaneConnections = [new HyperlaneConnection(HexDirection.NorthWest.RotateClockwise(rotation), HexDirection.SouthEast.RotateClockwise(rotation))]
                };
                map.Add(system);
                
                // Inner hyperlane
                system = new BoardHex()
                {
                    Coordinates = new HexCoordinates(0, -1).RotateClockwise(rotation),
                    HyperlaneConnections = [new HyperlaneConnection(HexDirection.SouthWest.RotateClockwise(rotation), HexDirection.SouthEast.RotateClockwise(rotation))]
                };
                map.Add(system);
            }
        }
        else if (game.Players.Count == 2)
        {
            // Bottom left
            var system = new BoardHex(HomeSystems.Random());
            system.Coordinates = new HexCoordinates(-3, 3);
            system.Planet!.OwningPlayerId = game.Players[0].GamePlayerId;
            map.Add(system);
            
            OuterSystems.Shuffle();
            system = new BoardHex(OuterSystems[0]);
            system.Coordinates = new HexCoordinates(-3, 2);
            map.Add(system);
            
            system = new BoardHex(OuterSystems[1]);
            system.Coordinates = new HexCoordinates(-2, 2);
            map.Add(system);
            
            system = new BoardHex(OuterSystems[2]);
            system.Coordinates = new HexCoordinates(-2, 3);
            map.Add(system);
            
            // Top right
            system = new BoardHex(HomeSystems.Random());
            system.Coordinates = new HexCoordinates(3, -3);
            system.Planet!.OwningPlayerId = game.Players[1].GamePlayerId;
            map.Add(system);
            
            OuterSystems.Shuffle();
            system = new BoardHex(OuterSystems[0]);
            system.Coordinates = new HexCoordinates(2, -3);
            map.Add(system);
            
            system = new BoardHex(OuterSystems[1]);
            system.Coordinates = new HexCoordinates(2, -2);
            map.Add(system);
            
            system = new BoardHex(OuterSystems[2]);
            system.Coordinates = new HexCoordinates(3, -2);
            map.Add(system);
            
            // Top left
            system = new BoardHex(OuterSystems.Random());
            system.Coordinates = new HexCoordinates(-1, -1);
            map.Add(system);
            
            // Bottom right
            system = new BoardHex(OuterSystems.Random());
            system.Coordinates = new HexCoordinates(1, 1);
            map.Add(system);
            
            // Centre
            system = new BoardHex(CenterSystems.Random());
            system.Coordinates = new HexCoordinates(0, 0);
            map.Add(system);
            
            system = new BoardHex(InnerSystems.Random());
            system.Coordinates = new HexCoordinates(0, -1);
            map.Add(system);
            
            system = new BoardHex(InnerSystems.Random());
            system.Coordinates = new HexCoordinates(+1, -1);
            map.Add(system);
            
            system = new BoardHex(InnerSystems.Random());
            system.Coordinates = new HexCoordinates(0, 1);
            map.Add(system);
            
            system = new BoardHex(InnerSystems.Random());
            system.Coordinates = new HexCoordinates(-1, 1);
            map.Add(system);
            
            // Hyperlanes
            // Top left
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(-3, 1),
                HyperlaneConnections = [new HyperlaneConnection(HexDirection.South, HexDirection.NorthEast)]
            };
            map.Add(system);
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(-2, 0),
                HyperlaneConnections = [new HyperlaneConnection(HexDirection.SouthWest, HexDirection.NorthEast)]
            };
            map.Add(system);
            
            // Top right
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(1, -3),
                HyperlaneConnections = [new HyperlaneConnection(HexDirection.SouthEast, HexDirection.SouthWest)]
            };
            map.Add(system);
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(0, -2),
                HyperlaneConnections = [new HyperlaneConnection(HexDirection.SouthWest, HexDirection.NorthEast)]
            };
            map.Add(system);
            
            // Bottom left
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(-1, 3),
                HyperlaneConnections = [new HyperlaneConnection(HexDirection.NorthWest, HexDirection.NorthEast)]
            };
            map.Add(system);
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(0, 2),
                HyperlaneConnections = [new HyperlaneConnection(HexDirection.SouthWest, HexDirection.NorthEast)]
            };
            map.Add(system);
            
            // Bottom right
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(2, 0),
                HyperlaneConnections = [new HyperlaneConnection(HexDirection.SouthWest, HexDirection.NorthEast)]
            };
            map.Add(system);
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(3, -1),
                HyperlaneConnections = [new HyperlaneConnection(HexDirection.SouthWest, HexDirection.North)]
            };
            map.Add(system);
        }
        
        return map;
    }
}