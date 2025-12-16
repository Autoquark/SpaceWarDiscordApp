using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.GameEvents.Movement;

[FirestoreData]
public abstract class GameEvent_MovementFlowComplete : GameEvent
{
    [FirestoreProperty]
    public required int PlayerGameId { get; set; }
    
    [FirestoreProperty]
    public required string? TriggerToMarkResolved { get; set; }
    
    [FirestoreProperty]
    public required List<SourceAndAmount> Sources { get; set; }
    
    [FirestoreProperty]
    public required HexCoordinates Destination {get; set;}
}

[FirestoreData]
public class GameEvent_MovementFlowComplete<T> : GameEvent_MovementFlowComplete
{
}