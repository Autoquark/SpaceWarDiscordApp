using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.GameEvents;

[FirestoreData]
public class GameEvent_AlterPlanet : GameEvent
{
    [FirestoreProperty]
    public required HexCoordinates Coordinates { get; set; }
    
    [FirestoreProperty]
    public required string? ResponsibleTechId { get; set; }

    [FirestoreProperty]
    public int ProductionChange { get; set; } = 0;

    [FirestoreProperty]
    public int ScienceChange { get; set; } = 0;

    [FirestoreProperty]
    public int StarsChange { get; set; } = 0;
}