using Google.Cloud.Firestore;

namespace Tumult.Database;

[FirestoreData]
public class BaseGamePlayer
{
    public const int GamePlayerIdNone = -1;

    [FirestoreProperty]
    public int GamePlayerId { get; set; } = -1;

    [FirestoreProperty]
    public ulong DiscordUserId { get; set; }
}
