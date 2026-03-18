using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.GameEvents;

/// <summary>
/// When this resolves, the given player gains the given amount of science
/// </summary>
[FirestoreData]
public class GameEvent_PlayerGainScience : GameEvent
{
    [FirestoreProperty]
    public required int PlayerGameId { get; set; }
    
    [FirestoreProperty]
    public required int Amount { get; set; }
}