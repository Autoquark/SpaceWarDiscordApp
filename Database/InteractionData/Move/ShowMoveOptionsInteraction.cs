using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData;

/// <summary>
/// Interaction that fires when the player wants to view all locations they can move to with a standard move action.
/// </summary>
[FirestoreData]
public class ShowMoveOptionsInteraction : InteractionData
{
    /// <summary>
    /// GameId of the player to show move options for
    /// </summary>
    [FirestoreProperty]
    public required int ForGamePlayerId { get; set; }
}