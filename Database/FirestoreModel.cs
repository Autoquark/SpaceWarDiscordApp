using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

[FirestoreData]
public class FirestoreModel
{
    [FirestoreDocumentId]
    public DocumentReference? DocumentId { get; set; }
}