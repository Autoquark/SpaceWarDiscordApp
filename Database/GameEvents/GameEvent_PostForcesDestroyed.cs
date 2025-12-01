using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.GameEvents;

public enum ForcesDestructionReason
{
    Combat,
    Tech,
    ExceedingCapacity
}

/// <summary>
/// Fires after forces have been destroyed
/// </summary>
[FirestoreData]
public class GameEvent_PostForcesDestroyed : GameEvent
{
    [FirestoreProperty]
    public required HexCoordinates Location { get; set; }
    
    [FirestoreProperty]
    public required int Amount { get; set; }
    
    [FirestoreProperty]
    public required int OwningPlayerGameId { get; set; }
    
    [FirestoreProperty]
    public required int ResponsiblePlayerGameId { get; set; }
    
    [FirestoreProperty]
    public required ForcesDestructionReason Reason { get; set; }
}