using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.CreativeAI;


[FirestoreData]
public class CreativeAITechInteraction : InteractionData
{
    [FirestoreProperty]
    public required string? TechId { get; set; }
}