using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.EnervatorBeam;

[FirestoreData]
public class UseEnervatorBeamInteraction : InteractionData
{
    [FirestoreProperty]
    public HexCoordinates Target { get; set; }
}