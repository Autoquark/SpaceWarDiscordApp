using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.Plagiarism;

[FirestoreData]
public class PlagiariseTechInteraction : InteractionData
{
    [FirestoreProperty]
    public required string TechId { get; set; }
}