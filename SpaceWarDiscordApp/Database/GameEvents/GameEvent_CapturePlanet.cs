using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.GameEvents;

/// <summary>
/// Fires after a player has captured a planet
/// (taken control of a planet that they did not previously control)
/// </summary>
[FirestoreData]
public class GameEvent_CapturePlanet : GameEvent
{
    [FirestoreProperty]
    public required int FormerOwnerGameId { get; set; } 
    
    [FirestoreProperty]
    public required HexCoordinates Location { get; set; }
}