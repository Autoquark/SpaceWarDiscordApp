using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.InteractionData.Tech;

namespace SpaceWarDiscordApp.Database.GameEvents;

/// <summary>
/// Prompts the given player to purchase a tech if they can afford one, or decline to do so
/// </summary>
[FirestoreData]
public class GameEvent_TechPurchaseDecision : GameEvent_PlayerChoice<PurchaseTechInteraction>
{
    [FirestoreProperty]
    public required int PlayerGameId { get; set; }
}