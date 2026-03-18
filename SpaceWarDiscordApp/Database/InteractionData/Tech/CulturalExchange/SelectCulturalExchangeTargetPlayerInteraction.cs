using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.CulturalExchange;

[FirestoreData]
public class SelectCulturalExchangeTargetPlayerInteraction : InteractionData
{
    [FirestoreProperty]
    public required int TargetPlayerId { get; set; }
}