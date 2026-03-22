using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents.Produce;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.PeoplePrinters;

[FirestoreData]
public class UsePeoplePrintersInteraction : EventModifyingInteractionData<GameEvent_BeginProduce>
{
    
}