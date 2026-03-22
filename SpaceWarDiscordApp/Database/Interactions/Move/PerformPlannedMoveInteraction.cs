using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.Interactions.Move;

[FirestoreData]
public class PerformPlannedMoveInteraction<T> : InteractionData
{
    [FirestoreProperty]
    public required string? TriggerToMarkResolvedId { get; set; }
}