
using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.GameEvents;

[FirestoreData]
public class GameEvent_TurnBegin : GameEvent
{
    [FirestoreProperty]
    public required int PlayerGameId { get; set; }
}