using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.AstralLandscaping;

[FirestoreData]
public class SelectAstralLandscapingSecondTargetInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates ControlledTarget { get; set; }
    
    [FirestoreProperty]
    public required HexCoordinates OtherTarget { get; set; }
}