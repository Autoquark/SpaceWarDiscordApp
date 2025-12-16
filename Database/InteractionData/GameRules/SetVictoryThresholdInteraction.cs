using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData.GameRules;

[FirestoreData]
public class SetVictoryThresholdInteraction : InteractionData
{
    [FirestoreProperty]
    public required int VictoryThreshold { get; set; }
}