using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech;

[FirestoreData]
public class ChoosePlayerStartingTechInteraction : InteractionData
{
    [FirestoreProperty]
    public required string TechId { get; set; }
}