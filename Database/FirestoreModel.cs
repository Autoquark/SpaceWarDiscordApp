using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.DatabaseModels;

[FirestoreData]
public class FirestoreModel
{
    [FirestoreDocumentId]
    public DocumentReference? DocumentId { get; set; }
}