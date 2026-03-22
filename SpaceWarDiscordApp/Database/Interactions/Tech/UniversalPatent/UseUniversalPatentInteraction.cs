using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.UniversalPatent;

[FirestoreData]
public class UseUniversalPatentInteraction : InteractionData
{
    [FirestoreProperty]
    public required string TechId { get; set; } = "";
}