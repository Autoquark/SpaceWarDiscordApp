using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.CrustalCrutoniser;

[FirestoreData]
public class UseCrustalCrutoniserInteraction : InteractionData
{
    [FirestoreProperty]
    public HexCoordinates Target { get; set; }
}