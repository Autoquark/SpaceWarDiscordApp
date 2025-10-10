using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData.GameRules;

[FirestoreData]
public class 
    SetStartingTechRuleInteraction : InteractionData
{
    [FirestoreProperty]
    public required StartingTechRule Value { get; set; }
}