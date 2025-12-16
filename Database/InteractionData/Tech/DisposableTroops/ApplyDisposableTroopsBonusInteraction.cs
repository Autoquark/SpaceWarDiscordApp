using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Produce;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.DisposableTroops;

[FirestoreData]
public class ApplyDisposableTroopsBonusInteraction : EventModifyingInteractionData<GameEvent_BeginProduce>
{
    
}