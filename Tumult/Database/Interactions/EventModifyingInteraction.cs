using Google.Cloud.Firestore;
using Tumult.Database.GameEvents;

namespace Tumult.Database.Interactions;

/// <summary>
/// Base class for interactions that trigger off an event of a specific type
/// </summary>
[FirestoreData]
public abstract class EventModifyingInteractionData<T> : EventModifyingInteractionData where T : GameEvent
{
    [FirestoreProperty]
    public required T Event { get; set; }
}
