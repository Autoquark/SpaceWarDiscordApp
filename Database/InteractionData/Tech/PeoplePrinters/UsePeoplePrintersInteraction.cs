using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.PeoplePrinters;

[FirestoreData]
public class UsePeoplePrintersInteraction : EventModifyingInteractionData<GameEvent_BeginProduce>
{
    
}