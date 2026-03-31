using Google.Cloud.Firestore;

namespace Tumult.Database.Interactions;

[FirestoreData]
public abstract class EventModifyingInteractionData : TriggeredEffectInteractionData
{
    [FirestoreProperty]
    public required string EventId { get; set; }
}
