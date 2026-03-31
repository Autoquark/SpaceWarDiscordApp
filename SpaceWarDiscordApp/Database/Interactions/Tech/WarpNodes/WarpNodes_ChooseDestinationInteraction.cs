using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.WarpNodes;

[FirestoreData]
public class WarpNodes_ChooseDestinationInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates? Destination { get; set; }
}