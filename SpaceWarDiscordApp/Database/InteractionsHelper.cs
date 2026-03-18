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

    public static List<string> SetUpInteractions(IEnumerable<InteractionData.InteractionData> interactions,
        Transaction transaction, ulong interactionGroupId) =>
        interactions.Select(x => SetUpInteraction(x, transaction, interactionGroupId)).ToList();

    public static string SetUpInteraction(InteractionData.InteractionData data,
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