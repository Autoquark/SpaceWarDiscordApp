using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.Interactions.GameRules;

[FirestoreData]
public class SetMapGeneratorInteraction : InteractionData
{
    [FirestoreProperty]
    public required string GeneratorId { get; set; }
}