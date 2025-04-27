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
public class BoardHex
{
    [FirestoreProperty]
    public Planet? Planet { get; set; }
    
    [FirestoreProperty]
    public HexCoordinates Coordinates { get; set; }
}