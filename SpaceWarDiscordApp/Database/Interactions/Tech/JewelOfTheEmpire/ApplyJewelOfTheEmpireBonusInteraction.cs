using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents.Produce;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.JewelOfTheEmpire;

[FirestoreData]
public class ApplyJewelOfTheEmpireBonusInteraction : EventModifyingInteractionData<GameEvent_BeginProduce>
{
}