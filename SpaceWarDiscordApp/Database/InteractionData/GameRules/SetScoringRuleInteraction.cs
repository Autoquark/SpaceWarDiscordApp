using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData.GameRules;

[FirestoreData]
public class SetScoringRuleInteraction : InteractionData
{
    [FirestoreProperty]
    public required ScoringRule Value { get; set; }
}