using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData.GameRules;

[FirestoreData]
public class SetMaxPlayerCountInteraction : InteractionData
{
    [FirestoreProperty]
    public int MaxPlayerCount { get; set; }
}