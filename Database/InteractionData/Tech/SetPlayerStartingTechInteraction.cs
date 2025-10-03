using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech;

[FirestoreData]
public class SetPlayerStartingTechInteraction : InteractionData
{
    [FirestoreProperty]
    public required string TechId { get; set; }
}