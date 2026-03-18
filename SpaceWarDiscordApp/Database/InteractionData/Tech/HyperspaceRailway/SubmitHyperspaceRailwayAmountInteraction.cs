using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.HyperspaceRailway;

[FirestoreData]
public class SubmitHyperspaceRailwayAmountInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates Source { get; set; }
    
    [FirestoreProperty]
    public required HexCoordinates Destination { get; set; }
    
    [FirestoreProperty]
    public required int Amount { get; set; }
}