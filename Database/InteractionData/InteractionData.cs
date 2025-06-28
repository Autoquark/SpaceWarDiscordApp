using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData;

[FirestoreData]
public abstract class InteractionData : PolymorphicFirestoreModel
{
    [FirestoreProperty]
    public string InteractionId { get; } = Guid.NewGuid().ToString();
    
    [FirestoreProperty]
    public required DocumentReference? Game { get; set; }

    /// <summary>
    /// GamePlayer ids of player that is expected to perform the interaction, or -1 if it's not player specific
    /// </summary>
    [FirestoreProperty]
    public required int ForGamePlayerId { get; set; }
    
    /// <summary>
    /// If true, the interaction response will be treated as an update to the original message
    /// (i.e. the root handler will respond with DeferredMessageUpdate instead of a DeferredChannelMessageWithSource,
    /// so that calling EditOriginalResponseAsync edits the message with the buttons instead of creating a new message)
    /// </summary>
    [FirestoreProperty]
    public bool EditOriginalMessage { get; set; } = false;
    
    [FirestoreProperty]
    public ulong InteractionGroupId { get; set; }

    public bool PlayerAllowedToTrigger(Game game, GamePlayer player)
        => ForGamePlayerId == -1
           || ForGamePlayerId == player.GamePlayerId
           || game.GetGamePlayerByGameId(ForGamePlayerId).IsDummyPlayer;
}