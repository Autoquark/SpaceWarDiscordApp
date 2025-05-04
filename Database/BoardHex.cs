using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.DatabaseModels;

public enum HexType
{
    Planet,
    Hyperlane,
    Impassible
}

[FirestoreData]
public class BoardHex
{
    public BoardHex() { }
    
    public BoardHex(BoardHex other)
    {
        if (other.Planet != null)
        {
            Planet = new Planet(other.Planet);
        }

        Coordinates = other.Coordinates;
    }
    
    [FirestoreProperty]
    public Planet? Planet { get; set; }
    
    [FirestoreProperty]
    public HexCoordinates Coordinates { get; set; }

    [FirestoreProperty]
    public IList<HyperlaneConnection> HyperlaneConnections { get; set; } = [];

    [FirestoreProperty]
    public bool HasAsteroids { get; set; } = false;
}

[FirestoreData]
public record struct HyperlaneConnection([property: FirestoreProperty] HexDirection First, [property: FirestoreProperty] HexDirection Second)
{
    
}