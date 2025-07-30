using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.GameEvents;

[FirestoreData]
public class GameEvent_MovementFlowComplete<T> : GameEvent
{
    [FirestoreProperty]
    public required int PlayerGameId { get; set; }
    
    [FirestoreProperty]
    public required string? TriggerToMarkResolved { get; set; }
}