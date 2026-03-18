using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData;

/// <summary>
/// Interaction sent when a player declines to trigger any further optional triggered
/// effects in response to a GameEvent
/// </summary>
[FirestoreData]
public class DeclineOptionalTriggersInteraction : InteractionData
{
    
}