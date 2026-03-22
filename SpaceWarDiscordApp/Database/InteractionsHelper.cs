using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

public static class InteractionsHelper
{
    public static async Task<GlobalData> GetGlobalDataAndIncrementInteractionGroupIdAsync() =>
        await Program.FirestoreDb.RunTransactionAsync(async transaction =>
        {
            var documentRef = transaction.Database.GlobalData();
            var globalData = (await transaction.GetSnapshotAsync(documentRef))
                .ConvertTo<GlobalData>() ?? new GlobalData
                {
                    DocumentId = documentRef
                };

            globalData.InteractionGroupId++;

            transaction.Set(globalData);
            return globalData;
        });

    public static List<string> SetUpInteractions(IEnumerable<InteractionData> interactions,
        Transaction transaction, ulong interactionGroupId) =>
        InteractionStatics.SetUpInteractions(interactions, transaction, transaction.Database.InteractionData(), interactionGroupId);

    public static string SetUpInteraction(InteractionData data,
        Transaction transaction, ulong interactionGroupId) =>
        InteractionStatics.SetUpInteraction(data, transaction, transaction.Database.InteractionData(), interactionGroupId);
}
