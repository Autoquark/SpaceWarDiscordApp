using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

public static class FirestoreDbExtensions
{
    /// <summary>
    /// Runs a transaction asynchronously, with a synchronous callback that doesn't return a value.
    /// </summary>
    /// <returns></returns>
    public static Task RunTransactionAsync(this FirestoreDb db, Action<Transaction> callback)
        => db.RunTransactionAsync(transaction =>
        {
            callback(transaction);
            return Task.CompletedTask;
        });
    
    /// <summary>
    /// Runs a transaction asynchronously, with a synchronous callback that returns a value.
    /// </summary>
    /// <returns></returns>
    public static Task<T> RunTransactionAsync<T>(this FirestoreDb db, Func<Transaction, T> callback)
        => db.RunTransactionAsync(transaction =>
        {
            var result = callback(transaction);
            return Task.FromResult(result);
        });
}