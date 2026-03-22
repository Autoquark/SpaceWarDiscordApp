using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.EnervatorBeam;

[FirestoreData]
public class UseEnervatorBeamInteraction : InteractionData
{
    [FirestoreProperty]
    public HexCoordinates Target { get; set; }
}