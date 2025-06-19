using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.EventRecords;

[FirestoreData]
public class MovementEventRecord : EventRecord
{
    [FirestoreProperty]
    public IList<SourceAndAmount> Sources { get; set; }
    
    [FirestoreProperty]
    public HexCoordinates Destination { get; set; }
}