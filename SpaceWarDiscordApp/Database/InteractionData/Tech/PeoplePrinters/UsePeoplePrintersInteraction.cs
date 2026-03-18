using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Produce;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.PeoplePrinters;

[FirestoreData]
public class UsePeoplePrintersInteraction : EventModifyingInteractionData<GameEvent_BeginProduce>
{
    
}