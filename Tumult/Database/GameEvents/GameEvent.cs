using Google.Cloud.Firestore;

namespace Tumult.Database.GameEvents;

/// <summary>
/// Game event being resolved that may trigger effects requiring player decisions.
/// </summary>
[FirestoreData]
public abstract class GameEvent : PolymorphicFirestoreModel
{
    [FirestoreProperty]
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    [FirestoreProperty]
    public List<TriggeredEffect> RemainingTriggersToResolve { get; set; } = [];

    [FirestoreProperty]
    public int ResolvingTriggersForPlayerId { get; set; } = -1;

    [FirestoreProperty]
    public List<int> PlayerIdsToResolveTriggersFor { get; set; } = [];

    [FirestoreProperty]
    public List<string> TriggerIdsResolved { get; set; } = [];

    public override string ToString()
        => $"{GetType().Name}: {EventId}, RemainingTriggersToResolve: {string.Join(", ", RemainingTriggersToResolve.Select(x => x.ToString()))}";
}
