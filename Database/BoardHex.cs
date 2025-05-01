using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.DatabaseModels;

public enum HexType
{
    Planet,
    Hyperlane,
    Impassible
}

[FirestoreData]
public record class BoardHex
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
}