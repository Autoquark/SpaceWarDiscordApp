using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database;

/// <summary>
/// Stores data about a move action that a player is currently planning
/// </summary>
[FirestoreData]
public class PlannedMove
{
    [FirestoreProperty]
    public HexCoordinates Destination { get; set; }
    
    [FirestoreProperty]
    public IList<SourceAndAmount> Sources { get; set; } = [];
}

[FirestoreData]
public class SourceAndAmount
{
    [FirestoreProperty]
    public required HexCoordinates Source { get; set; }
    
    [FirestoreProperty]
    public required int Amount { get; set; }
}