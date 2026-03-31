using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents.Produce;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.EliteTroops;

[FirestoreData]
public class ApplyEliteTroopsProductionReductionInteraction : EventModifyingInteractionData<GameEvent_BeginProduce>
{
    
}