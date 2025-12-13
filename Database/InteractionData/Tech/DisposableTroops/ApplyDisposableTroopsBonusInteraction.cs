using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.DisposableTroops;

[FirestoreData]
public class ApplyDisposableTroopsBonusInteraction : EventModifyingInteractionData<GameEvent_BeginProduce>
{
    
}