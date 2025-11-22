using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.GameEvents;

/// <summary>
/// When this resolves, the given player gains the given tech. Does not subtract any science points - for a purchase,
/// these will already have been subtracted.
/// </summary>
[FirestoreData]
public class GameEvent_PlayerGainTech : GameEvent
{
    [FirestoreProperty]
    public required int PlayerGameId { get; set; }
    
    [FirestoreProperty]
    public required string TechId { get; set; } = "";
}