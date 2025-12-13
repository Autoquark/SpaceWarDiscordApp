using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.DisposableTroops;

[FirestoreData]
public class DisposableTroopsDestroyForcesInteraction : EventModifyingInteractionData<GameEvent_PostProduce>
{
    
}