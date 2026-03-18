using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.DimensionalOrigami;

[FirestoreData]
public class UseDimensionalOrigamiInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates Target1 { get; set; }
    
    [FirestoreProperty]
    public required HexCoordinates Target2 { get; set; }
}