using System.Collections.Concurrent;
using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp;

public class GameSyncManager
{
    private readonly ConcurrentDictionary<DocumentReference, SemaphoreSlim> _locksByDocumentReference = new();
    
    public SemaphoreSlim GetSemaphoreForGame(Game game)
    {
        ArgumentNullException.ThrowIfNull(game.DocumentId);
        
        var newSemaphore = new SemaphoreSlim(1, 1);
        if (!_locksByDocumentReference.TryAdd(game.DocumentId, newSemaphore))
        {
            newSemaphore.Dispose();
        }
        
        _locksByDocumentReference.TryGetValue(game.DocumentId, out var semaphore);
        return semaphore!;
    }
}