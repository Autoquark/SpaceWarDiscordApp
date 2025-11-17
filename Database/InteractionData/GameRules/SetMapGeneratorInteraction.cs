using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData.GameRules;

[FirestoreData]
public class SetMapGeneratorInteraction : InteractionData
{
    [FirestoreProperty]
    public required string GeneratorId { get; set; }
}