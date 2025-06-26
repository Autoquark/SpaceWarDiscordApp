using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;

namespace SpaceWarDiscordApp.GameLogic.GameEvents;

/// <summary>
/// Effect triggered in response to a GameEvent
/// </summary>
public class TriggeredEffect
{
    /// <summary>
    /// Whether the effect must be resolved or is optional
    /// </summary>
    public bool IsMandatory { get; set; }

    /// <summary>
    /// Display name used e.g. on the button to resolve this effect
    /// </summary>
    public string DisplayName { get; set; } = "Unknown Effect";
    
    /// <summary>
    /// Reference to an InteractionData which can resolve this effect
    /// </summary>
    public DocumentReference ResolveInteraction { get; init; } = null!;
}