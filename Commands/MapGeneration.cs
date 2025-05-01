using SpaceWarDiscordApp.DatabaseModels;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Commands;

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
        
        /*var system = new BoardHex(HomeSystems.Random());
        system.Coordinates = new HexCoordinates(0, -1);
        system.Planet!.OwningPlayerId = game.Players[0].GamePlayerId;
        map.Add(system);

        
        game.Hexes = map;
        return map;*/
        //if (game.Players.Count == 4)
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
            system.Planet!.OwningPlayerId = game.Players[0].GamePlayerId;
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
            system.Planet!.OwningPlayerId = game.Players[0].GamePlayerId;
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
            system.Planet!.OwningPlayerId = game.Players[0].GamePlayerId;
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
            system.Coordinates = new HexCoordinates(+1, 0);
            map.Add(system);
            
            system = new BoardHex(InnerSystems.Random());
            system.Coordinates = new HexCoordinates(0, +1);
            map.Add(system);
            
            system = new BoardHex(InnerSystems.Random());
            system.Coordinates = new HexCoordinates(-1, +1);
            map.Add(system);
            
            system = new BoardHex(InnerSystems.Random());
            system.Coordinates = new HexCoordinates(-1, 0);
            map.Add(system);
        }

        game.Hexes = map;
        return map;
    }
}