using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData.GameRules;

[FirestoreData]
public class SetSingleUseTechCanBeUniversal : InteractionData
{
    [FirestoreProperty]
    public bool Value { get; set; } = false;
}