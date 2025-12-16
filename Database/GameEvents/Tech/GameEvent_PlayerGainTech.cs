using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.GameEvents.Tech;

/// <summary>
/// When this resolves, the given player gains the given tech. Does not subtract any science points - for a purchase,
/// these will already have been subtracted. Will remove the tech from the tech market if it is present. Optionally
/// cycles the tech market
/// </summary>
[FirestoreData]
public class GameEvent_PlayerGainTech : GameEvent
{
    [FirestoreProperty]
    public required int PlayerGameId { get; set; }
    
    [FirestoreProperty]
    public required string TechId { get; set; } = "";
    
    [FirestoreProperty]
    public required bool CycleMarket { get; set; }
    
    /// <summary>
    /// Optionally allows passing the PlayerTech object instead of allowing one to be created, allowing exhaustion status
    /// or other tech-specific data to be set.
    /// </summary>
    [FirestoreProperty]
    public PlayerTech? PlayerTech { get; set; } = null;
}