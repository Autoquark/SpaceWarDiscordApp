using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.Interactions.Tech;

[FirestoreData]
public class PurchaseTechInteraction : InteractionData
{
    [FirestoreProperty]
    public required string? TechId { get; set; }
    
    [FirestoreProperty]
    public required int Cost { get; set; }
}