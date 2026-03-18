using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.Psysploder;

[FirestoreData]
public class UsePsysploderInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates Target { get; set; }
    
    [FirestoreProperty]
    public required int Amount { get; set; }
}