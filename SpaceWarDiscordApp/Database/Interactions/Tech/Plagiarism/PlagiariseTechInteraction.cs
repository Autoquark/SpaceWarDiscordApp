using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.Plagiarism;

[FirestoreData]
public class PlagiariseTechInteraction : InteractionData
{
    [FirestoreProperty]
    public required string TechId { get; set; }
}