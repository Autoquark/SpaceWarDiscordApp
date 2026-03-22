using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents.Produce;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.DisposableTroops;

[FirestoreData]
public class DisposableTroopsDestroyForcesInteraction : EventModifyingInteractionData<GameEvent_PostProduce>
{
    
}