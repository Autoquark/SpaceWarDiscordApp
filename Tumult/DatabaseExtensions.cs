using Google.Cloud.Firestore;

namespace Tumult;

public static class DatabaseExtensions
{
    extension(FirestoreDb db)
    {
        public CollectionReference InteractionData() => db.Collection("InteractionData");
    }
}