using AsyncKeyedLock;
using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp;

public class GameSyncManager
{
    public readonly AsyncKeyedLocker<DocumentReference> Locker = new();
}