using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.GameEvents.Movement;

/// <summary>
/// When this resolves, movement of forces from one or more sources to a single destination is resolved.
/// Allows for modification of combat strength sources for both attacking and defending players.
/// </summary>
[FirestoreData]
public class GameEvent_PreMove : GameEvent
{
    [FirestoreProperty]
    public required int MovingPlayerId { get; set; }
    
    [FirestoreProperty]
    public required HexCoordinates Destination {get; set;}
    
    [FirestoreProperty]
    public required List<SourceAndAmount> Sources { get; set; }

    [FirestoreProperty]
    public List<CombatStrengthSource> AttackerCombatStrengthSources { get; set; } = [];
    
    [FirestoreProperty]
    public List<CombatStrengthSource> DefenderCombatStrengthSources { get; set; } = [];
    
    /// <summary>
    /// ID of the tech that is being used to perform this move, if any
    /// </summary>
    [FirestoreProperty]
    public string? TechId { get; set; } = null;
}

[FirestoreData]
public class CombatStrengthSource
{
    [FirestoreProperty]
    public required string DisplayName { get; set; } = "Unknown Source";
    
    [FirestoreProperty]
    public required int Amount { get; set; } = 0;

    public override string ToString() => $"{DisplayName}: {Amount}";
}