using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents.Movement;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.FlagOptimisation;

[FirestoreData]
public class ApplyFlagOptimisationBonusInteraction : TriggeredEffectInteractionData
{
    [FirestoreProperty]
    public required GameEvent_PreMove Event { get; set; }
    
    [FirestoreProperty]
    public required bool IsAttacker { get; set; }
}