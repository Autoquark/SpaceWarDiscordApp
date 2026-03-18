using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData;

/// <summary>
/// Equivalent to the reprompt command. Can be used as a 'cancel' button for backing out of any action planning / targeting
/// that hasn't actually altered the game state yet.
/// </summary>
[FirestoreData]
public class RepromptInteraction : InteractionData
{
}