using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData.Move;

[FirestoreData]
public class PerformPlannedMoveInteraction<T> : InteractionData
{
    [FirestoreProperty]
    public required string? TriggerToMarkResolvedId { get; set; }
}