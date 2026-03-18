using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.EventRecords;

[FirestoreData]
public class MovementEventRecord : EventRecord
{
    [FirestoreProperty]
    public required IList<SourceAndAmount> Sources { get; set; } = [];
    
    [FirestoreProperty]
    public required HexCoordinates Destination { get; set; }
    
    [FirestoreProperty]
    public required bool IsTechMove { get; set; }
}