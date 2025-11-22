using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.GameEvents;

/// <summary>
/// Fires after a player has produced on a planet
/// </summary>
[FirestoreData]
public class GameEvent_PostProduce : GameEvent
{
    [FirestoreProperty]
    public required int PlayerGameId { get; set; }
    
    [FirestoreProperty]
    public required int ForcesProduced { get; set; }
    
    [FirestoreProperty]
    public required int ScienceProduced { get; set; }
    
    [FirestoreProperty]
    public required HexCoordinates Location { get; set; }
}