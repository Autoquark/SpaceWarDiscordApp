using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData;

[FirestoreData]
public class ShowMoveOptionsInteraction : InteractionData
{
    /// <summary>
    /// GameId of the player to show move options for
    /// </summary>
    [FirestoreProperty]
    public required int ForGamePlayerId { get; set; }
}