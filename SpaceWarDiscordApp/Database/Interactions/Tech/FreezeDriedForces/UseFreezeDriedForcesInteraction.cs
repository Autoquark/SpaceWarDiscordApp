using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.FreezeDriedForces;

[FirestoreData]
public class UseFreezeDriedForcesInteraction : InteractionData
{
    [FirestoreProperty]
    public HexCoordinates Target { get; set; }
}