using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents.Produce;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.Isolationism;

[FirestoreData]
public class ApplyIsolationismBonusInteraction : EventModifyingInteractionData<GameEvent_BeginProduce>
{
    
}