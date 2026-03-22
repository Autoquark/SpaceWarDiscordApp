using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.HyperspaceRailway;

[FirestoreData]
public class SubmitHyperspaceRailwaySourceInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates Source { get; set; }
}