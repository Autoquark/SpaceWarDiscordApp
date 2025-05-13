using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.DatabaseModels;

namespace SpaceWarDiscordApp.GameLogic;

public static class BoardUtils
{
    public static ISet<BoardHex> GetStandardMoveSources(Game game, BoardHex destination, GamePlayer movingPlayer)
        => GetNeighbouringHexes(game, destination)
            .Where(x => x.Planet?.OwningPlayerId == movingPlayer.GamePlayerId)
            .ToHashSet(); 
    
    public static ISet<BoardHex> GetNeighbouringHexes(Game game, BoardHex hex)
    {
        var results = new HashSet<BoardHex>();
        
        // When exploring a hyperlane hex, we need to consider which neighbour we are coming from
        var toExplore = new Stack<(BoardHex hex, HexCoordinates from)>();
        foreach (var hexDirection in Enum.GetValues<HexDirection>())
        {
            var coordinates = hex.Coordinates + hexDirection;
            var neighbour = game.GetHexAt(coordinates);
            if (neighbour != null)
            {
                toExplore.Push((neighbour, coordinates));
            }
        }

        while (toExplore.Count > 0)
        {
            var (exploring, from) = toExplore.Pop();
            if (exploring.Planet != null)
            {
                results.Add(exploring);
                continue;
            }
            
            if (exploring.HyperlaneConnections.Any())
            {
                foreach (var connection in exploring.HyperlaneConnections)
                {
                    if (exploring.Coordinates + connection.First == from)
                    {
                        var neighbour = game.GetHexAt(exploring.Coordinates + connection.Second);
                        if (neighbour != null)
                        {
                            toExplore.Push((exploring, exploring.Coordinates));
                        }
                    }
                    else if (exploring.Coordinates + connection.Second == from)
                    {
                        var neighbour = game.GetHexAt(exploring.Coordinates + connection.First);
                        if (neighbour != null)
                        {
                            toExplore.Push((exploring, exploring.Coordinates));
                        }
                    }
                }
                continue;
            }
        }
        
        return results;
    }
}