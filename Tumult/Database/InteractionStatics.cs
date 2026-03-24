using Google.Cloud.Firestore;
using Tumult.Database.Interactions;

namespace Tumult.Database;

public static class InteractionStatics
{
    public static List<string> SetUpInteractions(IEnumerable<InteractionData> interactions,
        Transaction transaction, ulong interactionGroupId) =>
        interactions.Select(x => SetUpInteraction(x, transaction, interactionGroupId)).ToList();

    public static string SetUpInteraction(InteractionData data,
        Transaction transaction, ulong interactionGroupId)
    {
        if (string.IsNullOrEmpty(data.InteractionId))
        {
            throw new ArgumentException("InteractionId must be set");
        }

        var documentRef = transaction.Database.InteractionData().Document();
        data.DocumentId = documentRef;
        data.InteractionGroupId = interactionGroupId;
        transaction.Create(documentRef, data);
        return data.InteractionId;
    }
}
