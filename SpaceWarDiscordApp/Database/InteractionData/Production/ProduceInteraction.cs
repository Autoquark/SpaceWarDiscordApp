using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Production;

[FirestoreData]
public class ProduceInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates Hex {get; set;}
}