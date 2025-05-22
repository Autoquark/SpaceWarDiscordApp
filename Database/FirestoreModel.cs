using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

[FirestoreData]
public abstract class FirestoreModel
{
    [FirestoreDocumentId]
    public DocumentReference? DocumentId { get; set; }
}