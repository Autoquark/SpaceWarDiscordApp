using Google.Cloud.Firestore;
using SpaceWarDiscordApp.DatabaseModels;

namespace SpaceWarDiscordApp.Database;

public static class InteractionsHelper
{
    public static async Task<string> SetUpInteractionAsync(InteractionData.InteractionData data)
        => await Program.FirestoreDb.RunTransactionAsync(transaction =>
            Task.FromResult(SetUpInteraction(data, transaction)));

    public static string SetUpInteraction(InteractionData.InteractionData data,
        Transaction transaction)
    {
        var documentRef = transaction.Database.InteractionData().Document();
        data.DocumentId = documentRef;
        transaction.Create(documentRef, data);
        return data.InteractionId;
    }
}