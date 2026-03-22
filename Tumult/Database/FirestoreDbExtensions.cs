using Google.Cloud.Firestore;

namespace Tumult.Database;

public static class FirestoreDbExtensions
{
    /// <summary>
    /// Runs a transaction asynchronously, with a synchronous callback that doesn't return a value.
    /// </summary>
    public static Task RunTransactionAsync(this FirestoreDb db, Action<Transaction> callback, CancellationToken cancellationToken = default)
        => db.RunTransactionAsync(transaction =>
        {
            callback(transaction);
            return Task.CompletedTask;
        },
        cancellationToken: cancellationToken);

    /// <summary>
    /// Runs a transaction asynchronously, with a synchronous callback that returns a value.
    /// </summary>
    public static Task<T> RunTransactionAsync<T>(this FirestoreDb db, Func<Transaction, T> callback, CancellationToken cancellationToken = default)
        => db.RunTransactionAsync(transaction =>
        {
            var result = callback(transaction);
            return Task.FromResult(result);
        },
        cancellationToken: cancellationToken);
}
