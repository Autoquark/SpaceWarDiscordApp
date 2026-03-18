using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.WarpNodes;

[FirestoreData]
public class WarpNodes_ChooseAmountInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates Destination { get; set; }
    
    [FirestoreProperty]
    public required int Amount { get; set; }
    
    [FirestoreProperty]
    public required string ChoiceEventToResolve { get; set; }
}