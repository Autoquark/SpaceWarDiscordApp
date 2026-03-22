using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.AdaptiveAI;

[FirestoreData]
public class GainAdaptiveAIProgressInteraction : EventModifyingInteractionData<GameEvent_PostForcesDestroyed>
{
}