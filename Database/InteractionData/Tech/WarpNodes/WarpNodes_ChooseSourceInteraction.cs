using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.WarpNodes;

[FirestoreData]
public class WarpNodes_ChooseSourceInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates Source { get; set; }
}