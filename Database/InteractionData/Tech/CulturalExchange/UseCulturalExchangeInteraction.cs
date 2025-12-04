using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.CulturalExchange;

[FirestoreData]
public class UseCulturalExchangeInteraction : InteractionData
{
    [FirestoreProperty]
    public required int TargetGamePlayerId { get; set; }
    
    [FirestoreProperty]
    public required string TechId { get; set; }
}