using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData;

[FirestoreData]
public class PerformPlannedMoveInteraction : InteractionData
{
    [FirestoreProperty]
    public required int PlayerId { get; set; }
}