using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.GameEvents;

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