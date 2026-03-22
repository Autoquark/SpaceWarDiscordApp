using AsyncKeyedLock;
using Google.Cloud.Firestore;

namespace Tumult.GameLogic;

public class GameSyncManager
{
    public readonly AsyncKeyedLocker<DocumentReference> Locker = new();
}
