using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

[FirestoreData]
public class TechMarketSlot
{
    [FirestoreProperty]
    public required string? TechId { get; set; }
    
    [FirestoreProperty]
    public required int Cost { get; set; }
}