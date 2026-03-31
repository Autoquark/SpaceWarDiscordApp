using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents.Movement;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.AdaptiveAI;

[FirestoreData]
public class ApplyAdaptiveAIBonusInteraction : EventModifyingInteractionData<GameEvent_PreMove>
{
    [FirestoreProperty]
    public bool IsAttacker { get; set; }
}