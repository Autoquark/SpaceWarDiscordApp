using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.DimensionalOrigami;

[FirestoreData]
public class ChooseFirstDimensionalOrigamiSystemInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates Target { get; set; }
}