using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.GameEvents;

/// <summary>
/// When this resolves, forces on the given planet that are in excess of capacity will be removed.
/// </summary>
[FirestoreData]
public class GameEvent_ExceedingPlanetCapacity : GameEvent
{
    [FirestoreProperty]
    public required HexCoordinates Location { get; set; }
    
    [FirestoreProperty]
    public required int Capacity { get; set; }
}