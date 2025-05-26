using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Move;

/// <summary>
/// Fires when the player chooses a planet to move from and needs to now specify the amount of forces
/// </summary>
[FirestoreData]
public class AddMoveSourceInteraction<T> : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates Source { get; set; }
}