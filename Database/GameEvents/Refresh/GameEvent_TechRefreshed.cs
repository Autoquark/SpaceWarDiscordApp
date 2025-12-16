using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.GameEvents.Refresh;

[FirestoreData]
public class GameEvent_TechRefreshed : GameEvent
{
    [FirestoreProperty]
    public required int PlayerGameId { get; set; }
    
    [FirestoreProperty]
    public required string TechId { get; set; } = "";
}