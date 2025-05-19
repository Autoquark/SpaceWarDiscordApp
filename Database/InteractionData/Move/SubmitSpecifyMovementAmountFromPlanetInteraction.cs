using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData;

/// <summary>
/// Fires when the player submits a quantity of forces to move from a specific planet as part of planning a move action
/// </summary>
[FirestoreData]
public class SubmitSpecifyMovementAmountFromPlanetInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates From { get; set; }
    
    [FirestoreProperty]
    public required int Amount { get; set; }
    
    [FirestoreProperty]
    public required int MovingPlayerId { get; set; }
}