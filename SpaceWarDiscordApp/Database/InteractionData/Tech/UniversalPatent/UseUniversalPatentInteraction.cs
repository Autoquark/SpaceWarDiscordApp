using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.UniversalPatent;

[FirestoreData]
public class UseUniversalPatentInteraction : InteractionData
{
    [FirestoreProperty]
    public required string TechId { get; set; } = "";
}