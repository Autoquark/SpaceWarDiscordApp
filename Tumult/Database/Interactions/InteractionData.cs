using DSharpPlus.Entities;
using Google.Cloud.Firestore;

namespace Tumult.Database.Interactions;

[FirestoreData]
public abstract class InteractionData : PolymorphicFirestoreDocument
{
    [FirestoreProperty]
    public string InteractionId { get; set; } = Guid.NewGuid().ToString();

    [FirestoreProperty]
    public required DocumentReference? Game { get; set; }

    /// <summary>
    /// Optional reference to a GameEvent_PlayerChoice that this interaction relates to (it may not directly resolve it, name is legacy)
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
    /// </summary>
    [FirestoreProperty]
    public bool EditOriginalMessage { get; set; } = false;

    [FirestoreProperty]
    public bool EphemeralResponse { get; set; } = false;

    [FirestoreProperty]
    public ulong InteractionGroupId { get; set; }

    public bool UserAllowedToTrigger(BaseGame game, DiscordUser user)
    {
        if (game.IsDiscordUserInGame(user.Id))
        {
            if (ForGamePlayerId == -1 && ForDiscordUserId == 0)
            {
                return true;
            }

            if (ForGamePlayerId != -1)
            {
                return game.IsInteractionAllowedForUser(ForGamePlayerId, user.Id);
            }
        }

        return ForDiscordUserId == user.Id || ForDiscordUserId == 0;
    }
}
