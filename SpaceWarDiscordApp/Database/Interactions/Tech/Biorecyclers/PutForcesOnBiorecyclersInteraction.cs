using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.Biorecyclers;

[FirestoreData]
public class PutForcesOnBiorecyclersInteraction : EventModifyingInteractionData<GameEvent_PostForcesDestroyed>
{
}