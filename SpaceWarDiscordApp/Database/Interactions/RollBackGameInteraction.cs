using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.Interactions;

[FirestoreData]
public class RollBackGameInteraction : InteractionData
{
    [FirestoreProperty]
    public required DocumentReference BackupGameDocument { get; set; }
}