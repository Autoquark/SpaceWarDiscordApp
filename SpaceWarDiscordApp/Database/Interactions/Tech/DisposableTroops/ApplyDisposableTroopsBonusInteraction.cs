using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents.Produce;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.DisposableTroops;

[FirestoreData]
public class ApplyDisposableTroopsBonusInteraction : EventModifyingInteractionData<GameEvent_BeginProduce>
{
    
}