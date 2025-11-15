using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.InteractionData.Tech;

namespace SpaceWarDiscordApp.Database.GameEvents;

[FirestoreData]
public class GameEvent_TechPurchaseDecision : GameEvent_PlayerChoice<PurchaseTechInteraction>
{
    [FirestoreProperty]
    public required int PlayerGameId { get; set; }
}