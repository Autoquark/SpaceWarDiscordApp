using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Move;

/// <summary>
/// Fires when a player clicks a button to begin planning a move to a specific destination
/// </summary>
[FirestoreData]
public class SetMoveDestinationInteraction<T> : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates Destination { get; set; }
    
    [FirestoreProperty]
    public required HexCoordinates? FixedSource { get; set; }
    
    [FirestoreProperty]
    public required int? MaxAmountPerSource { get; set; }
    
    [FirestoreProperty]
    public required int? MinAmountPerSource { get; set; }
    
    [FirestoreProperty]
    public required string? TriggerToMarkResolvedId { get; set; }
}