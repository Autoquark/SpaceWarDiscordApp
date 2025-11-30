using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData;

[FirestoreData]
public class ShowRollBackConfirmInteraction : InteractionData
{
    // Referring to the backup by game document id is the safest way to ensure old buttons don't unexpectedly
    // do the wrong thing
    [FirestoreProperty]
    public required DocumentReference BackupGameDocument { get; set; }
}