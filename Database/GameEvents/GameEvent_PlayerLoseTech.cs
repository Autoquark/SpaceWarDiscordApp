using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.GameEvents;

/// <summary>
/// When this resolves, the given player loses the given tech.
/// </summary>
[FirestoreData]
public class GameEvent_PlayerLoseTech : GameEvent
{
    [FirestoreProperty]
    public required int PlayerGameId { get; set; }
    
    [FirestoreProperty]
    public required string TechId { get; set; } = "";
}