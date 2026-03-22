using Google.Cloud.Firestore;
using Tumult.Database.Interactions;

namespace Tumult.Database;

public static class InteractionStatics
{
    public static List<string> SetUpInteractions(IEnumerable<InteractionData> interactions,
        Transaction transaction, CollectionReference interactionDataCollection, ulong interactionGroupId) =>
        interactions.Select(x => SetUpInteraction(x, transaction, interactionDataCollection, interactionGroupId)).ToList();

    public static string SetUpInteraction(InteractionData data,
        Transaction transaction, CollectionReference interactionDataCollection, ulong interactionGroupId)
    {
        if (string.IsNullOrEmpty(data.InteractionId))
        {
            throw new ArgumentException("InteractionId must be set");
        }

        var documentRef = interactionDataCollection.Document();
        data.DocumentId = documentRef;
        data.InteractionGroupId = interactionGroupId;
        transaction.Create(documentRef, data);
        return data.InteractionId;
    }
}
