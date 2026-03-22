using Google.Cloud.Firestore;

namespace Tumult.Database;

[FirestoreData]
public abstract class FirestoreDocument
{
    [FirestoreDocumentId]
    public DocumentReference? DocumentId { get; set; }
}
