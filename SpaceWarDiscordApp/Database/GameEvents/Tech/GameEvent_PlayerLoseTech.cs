using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.GameEvents.Tech;

public enum LoseTechReason
{
    /// <summary>
    /// Losing the tech because it was single use
    /// </summary>
    SingleUse,
    /// <summary>
    /// Losing the tech due to effects of a tech (maybe itself) other than just being single use
    /// </summary>
    TechEffect
}

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
    
    [FirestoreProperty]
    public required LoseTechReason Reason { get; set; }
}