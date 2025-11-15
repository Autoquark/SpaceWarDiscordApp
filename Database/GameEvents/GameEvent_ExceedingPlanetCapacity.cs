using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.GameEvents;

[FirestoreData]
public class GameEvent_ExceedingPlanetCapacity : GameEvent
{
    [FirestoreProperty]
    public required HexCoordinates Location { get; set; }
    
    [FirestoreProperty]
    public required int Capacity { get; set; }
}