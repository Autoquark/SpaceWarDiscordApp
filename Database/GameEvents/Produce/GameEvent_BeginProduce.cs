using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.GameEvents.Produce;

/// <summary>
/// When this resolves forces/science points will be added.
/// Allows for manipulation of the amount of forces/science produced.
/// </summary>
[FirestoreData]
public class GameEvent_BeginProduce : GameEvent
{
    [FirestoreProperty]
    public required HexCoordinates Location { get; set; }
    
    [FirestoreProperty]
    public required int EffectiveProductionValue { get; set; }
    
    [FirestoreProperty]
    public required int EffectiveScienceProduction { get; set; }
    
    [FirestoreProperty]
    public required int? OverrideProducingPlayerId { get; set; }
}