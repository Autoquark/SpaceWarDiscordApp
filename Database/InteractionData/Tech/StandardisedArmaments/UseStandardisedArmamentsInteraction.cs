using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.StandardisedArmaments;

[FirestoreData]
public class UseStandardisedArmamentsInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates Target { get; set; }
}