using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents.Produce;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.EfficientManufacturing;

[FirestoreData]
public class ApplyEfficientManufacturingBonusInteraction : EventModifyingInteractionData<GameEvent_BeginProduce>
{
}