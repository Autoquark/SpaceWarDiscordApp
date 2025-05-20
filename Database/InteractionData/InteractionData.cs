using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData;

[FirestoreData]
public abstract class InteractionData : FirestoreModel
{
    protected InteractionData()
    {
        SubtypeName = GetType().FullName!;
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
    public required IList<int> AllowedGamePlayerIds { get; set; }
    
    /// <summary>
    /// If true, the interaction response will be treated as an update to the original message
    /// (i.e. the root handler will respond with DeferredMessageUpdate instead of a DeferredChannelMessageWithSource,
    /// so that calling EditOriginalResponseAsync edits the message with the buttons instead of creating a new message)
    /// </summary>
    [FirestoreProperty]
    public bool EditOriginalMessage { get; set; } = false;

    public bool PlayerAllowedToTrigger(GamePlayer player) 
        => !AllowedGamePlayerIds.Any() || AllowedGamePlayerIds.Contains(player.GamePlayerId);
}