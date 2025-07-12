using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.GameEvents;

[FirestoreData]
public class GameEvent_ActionComplete : GameEvent
{
    [FirestoreProperty]
    public required ActionType ActionType { get; set; }
}