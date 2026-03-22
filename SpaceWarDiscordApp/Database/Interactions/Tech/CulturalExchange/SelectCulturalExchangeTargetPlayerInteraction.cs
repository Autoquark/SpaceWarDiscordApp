using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.CulturalExchange;

[FirestoreData]
public class SelectCulturalExchangeTargetPlayerInteraction : InteractionData
{
    [FirestoreProperty]
    public required int TargetPlayerId { get; set; }
}