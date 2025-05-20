using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

public static class TransactionExtensions
{
    public static async Task<Game?> GetGameForChannelAsync(this Transaction transaction, ulong channelId) =>
        (await transaction.GetSnapshotAsync(
        new Query<Game>(transaction.Database.Games()).WhereEqualTo(x => x.GameChannelId, channelId)
            .Limit(1)))
            .FirstOrDefault()
            ?.ConvertTo<Game>();
    
    public static void Set(this Transaction transaction, FirestoreModel model) => transaction.Set(model.DocumentId, model);
}