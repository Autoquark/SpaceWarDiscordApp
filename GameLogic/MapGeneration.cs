using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.GameLogic;

public class MapGenerator
{
    private List<BoardHex> HomeSystems =
    [
        new()
        {
            Planet = new Planet
            {
                ForcesPresent = 3,
                Production = 3,
                Science = 1,
                Stars = 2,
                IsHomeSystem = true
            }
        }
    ];
    
    private List<BoardHex> OuterSystems =
    [
        new()
        {
            Planet = new Planet
            {
                Production = 3,
            }
        },
        new()
        {
            Planet = new Planet
            {
                Production = 1,
                Science = 1
            }
        },
        new()
        {
            Planet = new Planet
            {
                Production = 1,
                Stars = 1
            }
        }
    ];
    
    private List<BoardHex> InnerSystems =
    [
        new()
        {
            Planet = new Planet
            {
                Production = 3,
            }
        },
        new()
        {
            Planet = new Planet
            {
                Production = 2,
                Science = 1
            }
        },
        new()
        {
            Planet = new Planet
            {
                Production = 1,
                Stars = 1
            }
        },
    ];
    
    private List<BoardHex> CenterSystems =
    [
        new()
        {
            Planet = new Planet
            {
                Production = 6,
                Science = 1
            }
        },
        new()
        {
            Planet = new Planet
            {
                Stars = 2,
                Science = 1
            }
        },
        new()
        {
            Planet = new Planet
            {
                Science = 3
            }
        },
    ];
    
    
    public List<BoardHex> GenerateMap(Game game)
    {
        var map = new List<BoardHex>();
        
        if (game.Players.Count == 4)
        {
            // Slice 1 (top)
            var system = new BoardHex(HomeSystems.Random());
            system.Coordinates = new HexCoordinates(0, -3);
            system.Planet!.OwningPlayerId = game.Players[0].GamePlayerId;
            map.Add(system);
            
            system = new BoardHex(OuterSystems.Random());
            system.Coordinates = new HexCoordinates(-1, -2);
            map.Add(system);
            
            system = new BoardHex(OuterSystems.Random());
            system.Coordinates = new HexCoordinates(0, -2);
            map.Add(system);
            
            system = new BoardHex(OuterSystems.Random());
            system.Coordinates = new HexCoordinates(1, -3);
            map.Add(system);
            
            // Slice 2 (top right)
            system = new BoardHex(HomeSystems.Random());
            system.Coordinates = new HexCoordinates(3, -3);
            system.Planet!.OwningPlayerId = game.Players[1].GamePlayerId;
            map.Add(system);
            
            system = new BoardHex(OuterSystems.Random());
            system.Coordinates = new HexCoordinates(2, -3);
            map.Add(system);
            
            system = new BoardHex(OuterSystems.Random());
            system.Coordinates = new HexCoordinates(2, -2);
            map.Add(system);
            
            system = new BoardHex(OuterSystems.Random());
            system.Coordinates = new HexCoordinates(3, -2);
            map.Add(system);
            
            // Slice 3 (bottom left)
            system = new BoardHex(HomeSystems.Random());
            system.Coordinates = new HexCoordinates(-3, 3);
            system.Planet!.OwningPlayerId = game.Players[2].GamePlayerId;
            map.Add(system);
            
            system = new BoardHex(OuterSystems.Random());
            system.Coordinates = new HexCoordinates(-3, 2);
            map.Add(system);
            
            system = new BoardHex(OuterSystems.Random());
            system.Coordinates = new HexCoordinates(-2, 2);
            map.Add(system);
            
            system = new BoardHex(OuterSystems.Random());
            system.Coordinates = new HexCoordinates(-2, 3);
            map.Add(system);
            
            // Slice 4 (bottom)
            system = new BoardHex(HomeSystems.Random());
            system.Coordinates = new HexCoordinates(0, 3);
            system.Planet!.OwningPlayerId = game.Players[3].GamePlayerId;
            map.Add(system);
            
            system = new BoardHex(OuterSystems.Random());
            system.Coordinates = new HexCoordinates(-1, 3);
            map.Add(system);
            
            system = new BoardHex(OuterSystems.Random());
            system.Coordinates = new HexCoordinates(0, 2);
            map.Add(system);
            
            system = new BoardHex(OuterSystems.Random());
            system.Coordinates = new HexCoordinates(1, 2);
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
            system.Coordinates = new HexCoordinates(0, +1);
            map.Add(system);
            
            system = new BoardHex(InnerSystems.Random());
            system.Coordinates = new HexCoordinates(-1, +1);
            map.Add(system);
            
            // Hyperlanes
            // Left side
            // Outer edge
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(-2, -1),
                HyperlaneConnections = [new HyperlaneConnection(HexDirection.NorthEast, HexDirection.SouthWest)]
            };
            map.Add(system);
            
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(-3, 0),
                HyperlaneConnections = [new HyperlaneConnection(HexDirection.NorthEast, HexDirection.South)]
            };
            map.Add(system);
            
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(-3, 1),
                HyperlaneConnections = [new HyperlaneConnection(HexDirection.North, HexDirection.South)]
            };
            map.Add(system);
            
            // Inner
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(-1, -1),
                HyperlaneConnections = [new HyperlaneConnection(HexDirection.North, HexDirection.SouthEast)]
            };
            map.Add(system);
            
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(-1, 0),
                HyperlaneConnections = [new HyperlaneConnection(HexDirection.NorthEast, HexDirection.South)]
            };
            map.Add(system);
            
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(-2, 1),
                HyperlaneConnections = [new HyperlaneConnection(HexDirection.SouthWest, HexDirection.SouthEast)]
            };
            map.Add(system);
            
            // Right side
            // Outer edge
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(3, -1),
                HyperlaneConnections = [new HyperlaneConnection(HexDirection.North, HexDirection.South)]
            };
            map.Add(system);
            
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(3, 0),
                HyperlaneConnections = [new HyperlaneConnection(HexDirection.North, HexDirection.SouthWest)]
            };
            map.Add(system);
            
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(2, 1),
                HyperlaneConnections = [new HyperlaneConnection(HexDirection.NorthEast, HexDirection.SouthWest)]
            };
            map.Add(system);
            
            // Inner
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(1, 1),
                HyperlaneConnections = [new HyperlaneConnection(HexDirection.NorthWest, HexDirection.South)]
            };
            map.Add(system);
            
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(1, 0),
                HyperlaneConnections = [new HyperlaneConnection(HexDirection.North, HexDirection.SouthWest)]
            };
            map.Add(system);
            
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(2, -1),
                HyperlaneConnections = [new HyperlaneConnection(HexDirection.NorthWest, HexDirection.NorthEast)]
            };
            map.Add(system);
            
            // Asteroids
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(-2, 0),
                HasAsteroids = true
            };
            map.Add(system);
            
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(1, -2),
                HasAsteroids = true
            };
            map.Add(system);
            
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(-1, 2),
                HasAsteroids = true
            };
            map.Add(system);
            
            system = new BoardHex()
            {
                Coordinates = new HexCoordinates(2, 0),
                HasAsteroids = true
            };
            map.Add(system);
        }
        
        var distinct = map.Select(x => x.Coordinates).Distinct();
        var duplicate = map.Select(x => x.Coordinates).Except(distinct).ToList();
        if (duplicate.Any())
        {
            throw new Exception("Bad map generated! Duplicate hex coordinates: " + string.Join(", ", duplicate));
        }
        game.Hexes = map;
        return map;
    }
}