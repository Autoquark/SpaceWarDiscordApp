using DSharpPlus.Entities;
using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData;

[FirestoreData]
public abstract class InteractionData : PolymorphicFirestoreDocument
{
    [FirestoreProperty]
    public string InteractionId { get; set; } = Guid.NewGuid().ToString();
    
    [FirestoreProperty]
    public required DocumentReference? Game { get; set; }
    
    /// <summary>
    /// Optional reference to a GameEvent_PlayerChoice that is resolved by this interaction
    /// </summary>
    [FirestoreProperty]
    public string? ResolvesChoiceEventId { get; set; }

    /// <summary>
    /// GamePlayer id of the player that is allowed to perform the interaction, or -1 if it's not player specific
    /// </summary>
    [FirestoreProperty]
    public required int ForGamePlayerId { get; set; }
    
    /// <summary>
    /// Discord user id of the user that is allowed to perform this interaction. For interactions that can be used by
    /// users who are not part of the game already.
    /// </summary>
    [FirestoreProperty]
    public ulong ForDiscordUserId { get; set; }
    
    /// <summary>
    /// If true, the interaction response will be treated as an update to the original message
    /// (i.e. the root handler will respond with DeferredMessageUpdate instead of a DeferredChannelMessageWithSource,
    /// so that calling EditOriginalResponseAsync edits the message with the buttons instead of creating a new message)
    /// </summary>
    [FirestoreProperty]
    public bool EditOriginalMessage { get; set; } = false;
    
    [FirestoreProperty]
    public bool EphemeralResponse { get; set; } = false;
    
    [FirestoreProperty]
    public ulong InteractionGroupId { get; set; }

    public bool UserAllowedToTrigger(Game game, DiscordUser user)
    {
        var player = game.TryGetGamePlayerByDiscordId(user.Id);
        
        // If the user trying to trigger is a player in this game
        if (player != null)
        {
            // If this interaction is for anyone in the game
            if (ForGamePlayerId == -1 && ForDiscordUserId == 0)
            {
                return true;
            }

            // For a specific user in the game
            if (ForGamePlayerId != -1)
            {
                var forPlayer = game.GetGamePlayerByGameId(ForGamePlayerId);
                // If this interaction is for this player
                if (forPlayer == player)
                {
                    return true;
                }

                // Any player can trigger on behalf of a dummy player
                if (forPlayer.IsDummyPlayer)
                {
                    return true;
                }
                
                return false;
            }
        }

        // For a specific discord user not in the game or for any user
        return ForDiscordUserId == user.Id || ForDiscordUserId == 0;
    }
}