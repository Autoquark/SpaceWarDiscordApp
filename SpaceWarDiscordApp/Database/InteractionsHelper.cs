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
}
