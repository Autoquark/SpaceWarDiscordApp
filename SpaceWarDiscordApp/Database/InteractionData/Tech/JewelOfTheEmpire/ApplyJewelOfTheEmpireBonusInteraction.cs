using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Produce;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.JewelOfTheEmpire;

[FirestoreData]
public class ApplyJewelOfTheEmpireBonusInteraction : EventModifyingInteractionData<GameEvent_BeginProduce>
{
}