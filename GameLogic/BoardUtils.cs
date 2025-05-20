using SpaceWarDiscordApp.Database;

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
                toExplore.Push((neighbour, hex.Coordinates));
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
                    var firstEnd = exploring.Coordinates + connection.First;
                    var secondEnd = exploring.Coordinates + connection.Second;
                    if (firstEnd == from)
                    {
                        var neighbour = game.GetHexAt(secondEnd);
                        if (neighbour != null)
                        {
                            toExplore.Push((neighbour, exploring.Coordinates));
                        }
                    }
                    else if (secondEnd == from)
                    {
                        var neighbour = game.GetHexAt(firstEnd);
                        if (neighbour != null)
                        {
                            toExplore.Push((neighbour, exploring.Coordinates));
                        }
                    }
                }
            }
        }
        
        return results;
    }
}