using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.AstralLandscaping;

[FirestoreData]
public class SelectAstralLandscapingFirstTargetInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates Target { get; set; }
}