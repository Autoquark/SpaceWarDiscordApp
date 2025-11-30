using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

[FirestoreData]
public class RollbackState
{
    [FirestoreProperty]
    public required DocumentReference GameDocument { get; set; }
    
    [FirestoreProperty]
    public required int TurnNumber { get; set; }
    
    [FirestoreProperty]
    public required int CurrentTurnGamePlayerId { get; set; }
}