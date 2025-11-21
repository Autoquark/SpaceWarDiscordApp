using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

[FirestoreData]
public abstract class FirestoreDocument
{
    [FirestoreDocumentId]
    public DocumentReference? DocumentId { get; set; }
}