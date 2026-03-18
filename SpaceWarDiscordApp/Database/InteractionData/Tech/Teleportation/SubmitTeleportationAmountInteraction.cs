using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.Teleportation;

[FirestoreData]
public class SubmitTeleportationAmountInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates Source { get; set; }
    
    [FirestoreProperty]
    public required HexCoordinates Destination { get; set; }
    
    [FirestoreProperty]
    public required int Amount { get; set; }
}