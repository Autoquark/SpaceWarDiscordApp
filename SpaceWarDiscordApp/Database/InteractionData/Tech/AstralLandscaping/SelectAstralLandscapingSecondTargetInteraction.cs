using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.AstralLandscaping;

[FirestoreData]
public class SelectAstralLandscapingSecondTargetInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates ControlledTarget { get; set; }
    
    [FirestoreProperty]
    public required HexCoordinates OtherTarget { get; set; }
}