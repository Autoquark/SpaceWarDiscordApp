using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.AdaptiveAI;

[FirestoreData]
public class ApplyAdaptiveAIBonusInteraction : EventModifyingInteractionData<GameEvent_PreMove>
{
    [FirestoreProperty]
    public bool IsAttacker { get; set; }
}