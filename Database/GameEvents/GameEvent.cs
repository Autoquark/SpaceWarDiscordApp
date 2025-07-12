using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic.GameEvents;

namespace SpaceWarDiscordApp.Database.GameEvents;

/// <summary>
/// Game event being resolved that may trigger effects requiring player decisions.
/// Tracks any event-specific data that may change as a result of triggered effects
/// e.g. Combat Strength for a combat event.
/// </summary>
[FirestoreData]
public abstract class GameEvent : PolymorphicFirestoreModel
{
    [FirestoreProperty]
    public List<TriggeredEffect> RemainingTriggersToResolve { get; set; } = [];

    [FirestoreProperty]
    public int ResolvingTriggersForPlayerId { get; set; } = -1;
    
    [FirestoreProperty]
    public List<int> PlayerIdsToResolveTriggersFor { get; set; } = [];
}