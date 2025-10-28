using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.EfficientManufacturing;

[FirestoreData]
public class ApplyEfficientManufacturingBonusInteraction : EventModifyingInteractionData<GameEvent_BeginProduce>
{
}