using Google.Cloud.Firestore;
using SpaceWarDiscordApp.DatabaseModels;

namespace SpaceWarDiscordApp.Database.InteractionData;

[FirestoreData]
public abstract class InteractionData : FirestoreModel
{
    protected InteractionData()
    {
        SubtypeName = GetType().FullName;
    }
    
    [FirestoreProperty]
    public string SubtypeName { get; set; }

    [FirestoreProperty]
    public string InteractionId { get; } = Guid.NewGuid().ToString();
    
    [FirestoreProperty]
    public required DocumentReference? Game { get; set; }

    /// <summary>
    /// GamePlayer ids of players that are allowed to trigger this interaction. Empty means any player
    /// </summary>
    [FirestoreProperty]
    public IList<int> AllowedGamePlayerIds { get; set; } = [];

    public bool PlayerAllowedToTrigger(GamePlayer player) 
        => !AllowedGamePlayerIds.Any() || AllowedGamePlayerIds.Contains(player.GamePlayerId);
}