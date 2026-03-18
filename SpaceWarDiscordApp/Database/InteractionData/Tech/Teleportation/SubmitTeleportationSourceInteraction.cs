using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.Teleportation;

[FirestoreData]
public class SubmitTeleportationSourceInteraction : InteractionData
{
    [FirestoreProperty]
    public HexCoordinates Source { get; set; }
}