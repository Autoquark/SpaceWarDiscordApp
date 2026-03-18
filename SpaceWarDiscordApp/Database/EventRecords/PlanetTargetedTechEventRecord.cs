using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.EventRecords;

[FirestoreData]
public class PlanetTargetedTechEventRecord : EventRecord
{
    [FirestoreProperty]
    public required HexCoordinates Coordinates { get; set; }
}