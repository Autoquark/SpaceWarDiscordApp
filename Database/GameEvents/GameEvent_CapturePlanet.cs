using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.GameEvents;

[FirestoreData]
public class GameEvent_CapturePlanet : GameEvent
{
    [FirestoreProperty]
    public required int FormerOwnerGameId { get; set; } 
    
    [FirestoreProperty]
    public required HexCoordinates Location { get; set; }
}