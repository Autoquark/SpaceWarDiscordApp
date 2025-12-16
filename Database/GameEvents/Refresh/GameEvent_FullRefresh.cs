using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.GameEvents.Refresh;

[FirestoreData]
public class GameEvent_FullRefresh : GameEvent
{
    [FirestoreProperty]
    public required int GamePlayerId { get; set; }
}