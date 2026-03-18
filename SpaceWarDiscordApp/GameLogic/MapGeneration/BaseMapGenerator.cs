using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.GameLogic.MapGeneration;

public abstract class BaseMapGenerator
{
    protected BaseMapGenerator(string id, string displayName, params IEnumerable<int> supportedPlayerCounts)
    {
        DisplayName = displayName;
        SupportedPlayerCounts = supportedPlayerCounts.ToList();
        Id = id;
        
        if (SupportedPlayerCounts.Count == 0)
        {
            throw new Exception($"Map generator {DisplayName} has no supported player counts.");
        }
        if (!GeneratorsById.TryAdd(id, this))
        {
            throw new Exception($"Map generator {DisplayName} already registered");
        }
    }
    
    public static BaseMapGenerator GetGenerator(string id) => GeneratorsById[id];
    
    public static IEnumerable<BaseMapGenerator> GetAllGenerators() => GeneratorsById.Values;
    
    private static Dictionary<string, BaseMapGenerator> GeneratorsById { get; } = new();
    
    public string Id { get; }
    public string DisplayName { get; }

    public string Description { get; init; } = "";
    
    public IReadOnlyCollection<int> SupportedPlayerCounts { get; set; }

    public List<BoardHex> GenerateMap(Game game)
    {
        var hexes = GenerateMapInternal(game);
        var distinct = hexes.DistinctBy(x => x.Coordinates);
        var duplicate = hexes.Except(distinct).ToList();
        if (duplicate.Any())
        {
            throw new Exception("Bad map generated! Duplicate hex coordinates: " + string.Join(", ", duplicate.Select(x => x.Coordinates.ToCoordsString())));
        }

        return hexes;
    }
    public abstract List<BoardHex> GenerateMapInternal(Game game);
    
    // Standard systems for given board areas. Subclasses may change or ignore these
    
    protected List<BoardHex> HomeSystems =
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
    
    protected List<BoardHex> OuterSystems =
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
    
    protected List<BoardHex> InnerSystems =
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
    
    protected List<BoardHex> CenterSystems =
    [
        /*new()
        {
            Planet = new Planet
            {
                Production = 6,
                Science = 1
            }
        },*/
        new()
        {
            Planet = new Planet
            {
                Stars = 2,
                Science = 1
            }
        },
        /*new()
        {
            Planet = new Planet
            {
                Science = 3
            }
        },*/
    ];
}