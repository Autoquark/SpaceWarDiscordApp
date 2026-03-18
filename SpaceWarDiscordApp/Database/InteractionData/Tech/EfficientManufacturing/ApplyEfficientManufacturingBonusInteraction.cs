using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Produce;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.EfficientManufacturing;

[FirestoreData]
public class ApplyEfficientManufacturingBonusInteraction : EventModifyingInteractionData<GameEvent_BeginProduce>
{
}