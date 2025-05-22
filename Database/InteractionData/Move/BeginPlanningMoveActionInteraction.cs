using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Move;

/// <summary>
/// Fires when a player clicks a button to begin planning a move to a specific destination
/// </summary>
[FirestoreData]
public class BeginPlanningMoveActionInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates Destination { get; set; }
}