using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.Interactions.GameRules;

[FirestoreData]
public class SetTechMarketRuleInteraction : InteractionData
{
    [FirestoreProperty]
    public required TechMarketRule Value { get; set; }
}