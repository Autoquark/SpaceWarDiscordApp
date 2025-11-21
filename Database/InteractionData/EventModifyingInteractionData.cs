using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData;

[FirestoreData]
public abstract class EventModifyingInteractionData : TriggeredEffectInteractionData
{
    [FirestoreProperty]
    public required string EventId { get; set; }
}