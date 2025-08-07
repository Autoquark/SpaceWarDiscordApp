using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;

namespace SpaceWarDiscordApp.GameLogic.GameEvents;

/// <summary>
/// Effect triggered in response to a GameEvent
/// </summary>
[FirestoreData]
public class TriggeredEffect
{
    /// <summary>
    /// If true, this trigger will be resolved immediately; the player will not be prompted for resolution order, and if multiple
    /// AlwaysAutoResolve effects trigger at the same time, their resolution order is arbitrary
    /// </summary>
    [FirestoreProperty]
    public required bool AlwaysAutoResolve { get; set; }
    
    /// <summary>
    /// Whether the effect must be resolved or is optional
    /// </summary>
    [FirestoreProperty]
    public required bool IsMandatory { get; set; }

    /// <summary>
    /// Display name used e.g. on the button to resolve this effect
    /// </summary>
    [FirestoreProperty]
    public required string DisplayName { get; set; } = "Unknown Effect";

    /// <summary>
    /// Reference to an InteractionData which can resolve this effect
    /// </summary>
    [FirestoreProperty]
    public string ResolveInteractionId { get; set; } = "";
    
    // InteractionData to resolve this effect. Used to avoid going via firestore when an effect is created and then
    // resolved immediately.
    public required TriggeredEffectInteractionData? ResolveInteractionData { get; set; }
}