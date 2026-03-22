using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.CreativeAI;


[FirestoreData]
public class CreativeAITechInteraction : InteractionData
{
    [FirestoreProperty]
    public required string? TechId { get; set; }
}