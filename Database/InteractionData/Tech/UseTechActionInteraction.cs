using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData;

[FirestoreData]
public class UseTechActionInteraction : InteractionData
{
    [FirestoreProperty]
    public required string TechId { get; set; }
    
    [FirestoreProperty]
    public required string ActionId { get; set; }
    
    [FirestoreProperty]
    public required int UsingPlayerId { get; set; }
}