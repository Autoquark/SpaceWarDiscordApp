using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.Interactions.GameRules;

[FirestoreData]
public class SetMaxPlayerCountInteraction : InteractionData
{
    [FirestoreProperty]
    public int MaxPlayerCount { get; set; }
}