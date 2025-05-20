using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData.Move;

[FirestoreData]
public class PerformPlannedMoveInteraction : InteractionData
{
    [FirestoreProperty]
    public required int PlayerId { get; set; }
}