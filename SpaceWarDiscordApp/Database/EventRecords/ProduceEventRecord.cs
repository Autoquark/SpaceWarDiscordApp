using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.EventRecords;

[FirestoreData]
public class ProduceEventRecord : EventRecord
{
    [FirestoreProperty]
    public HexCoordinates Coordinates { get; set; }
}