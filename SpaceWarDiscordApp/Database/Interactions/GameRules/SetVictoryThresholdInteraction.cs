using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.Interactions.GameRules;

[FirestoreData]
public class SetVictoryThresholdInteraction : InteractionData
{
    [FirestoreProperty]
    public required int VictoryThreshold { get; set; }
}