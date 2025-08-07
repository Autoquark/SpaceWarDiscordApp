using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.InteractionData;

[FirestoreData]
public abstract class EventModifyingInteractionData<T> : EventModifyingInteractionData where T : GameEvent
{
    [FirestoreProperty]
    public required T Event { get; set; }
}