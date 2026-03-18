using System.Diagnostics;
using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.GameLogic.MapGeneration;

public class RandomGridMapGenerator : BaseMapGenerator
{
    public RandomGridMapGenerator() : base("random-grid", "insanity", CollectionExtensions.Between(2, 6))
    {
        Description = "A grid of random planets, with no special centre planet and random home system locations. Don't expect fairness.";
    }

    private static readonly List<int> HeightByPlayerCount = [3, 4, 5, 5, 6];
    private static readonly List<int> WidthByPlayerCount = [3, 4, 4, 5, 5];
    private static readonly List<(int value, float weight)> ProductionValueWeights = [(0, 0.1f), (1, 0.35f), (2, 0.35f), (3, 0.25f), (4, 0.05f)];
    private static readonly List<(int value, float weight)> ScienceValueWeights = [(0, 0.70f), (1, 0.25f), (2, 0.05f)];
    private static readonly List<(int value, float weight)> StarValueWeights = [(0, 0.70f), (1, 0.25f), (2, 0.05f)];

    public override List<BoardHex> GenerateMapInternal(Game game)
    {
        var height = HeightByPlayerCount[game.Players.Count - 2];
        var width = WidthByPlayerCount[game.Players.Count - 2];
        
        var hexes = new List<BoardHex>();

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var parity = y & 1;
                var q = y;
                var r = x - (y - parity) / 2;
                var coordinate = new HexCoordinates(q, r);
                hexes.Add(new BoardHex
                {
                    Coordinates = coordinate,
                    Planet = new Planet
                    {
                        Production = ProductionValueWeights.Random(tuple => tuple.weight).value,
                        Science = ScienceValueWeights.Random(tuple => tuple.weight).value,
                        Stars = StarValueWeights.Random(tuple => tuple.weight).value,
                    }
                });
            }
        }

        // Replace random hexes with player home systems, requiring them to be non-adjacent
        var canReplace = hexes.ToList();
        var replaced = 0;
        do
        {
            var toReplace = canReplace.Random();

            var homeSystem = new BoardHex(HomeSystems.Random())
            {
                Coordinates = toReplace.Coordinates,
            };
            homeSystem.Planet!.OwningPlayerId = game.Players[replaced].GamePlayerId;
            
            hexes.Remove(toReplace);
            hexes.Add(homeSystem);
            
            canReplace.Remove(toReplace);
            var neighbours = toReplace.Coordinates.GetNeighbors().ToList();
            canReplace = canReplace.Where(x => !neighbours.Contains(x.Coordinates)).ToList();
            
            replaced++;
        } while (replaced < game.Players.Count);

        return hexes;
    }
}