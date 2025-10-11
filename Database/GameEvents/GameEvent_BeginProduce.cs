using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.GameEvents;

[FirestoreData]
public class GameEvent_BeginProduce : GameEvent
{
    [FirestoreProperty]
    public required HexCoordinates Location { get; set; }
    
    [FirestoreProperty]
    public required int EffectiveProductionValue { get; set; }
    
    [FirestoreProperty]
    public required int EffectiveScienceProduction { get; set; }
}