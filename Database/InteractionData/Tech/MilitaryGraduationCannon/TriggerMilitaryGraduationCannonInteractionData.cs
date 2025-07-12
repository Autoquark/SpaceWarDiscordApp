using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.MilitaryGraduationCannon;

[FirestoreData]
public class TriggerMilitaryGraduationCannonInteractionData : TriggeredEffectInteractionData
{
    [FirestoreProperty]
    public required HexCoordinates Source { get; set; }
    
    [FirestoreProperty]
    public required int AmountProduced { get; set; }
}