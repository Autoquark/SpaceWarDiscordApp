using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.AdaptiveAI;

[FirestoreData]
public class GainAdaptiveAIProgressInteraction : EventModifyingInteractionData<GameEvent_PostForcesDestroyed>
{
}