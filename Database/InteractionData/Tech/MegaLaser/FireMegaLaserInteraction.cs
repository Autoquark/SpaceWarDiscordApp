using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.MegaLaser;

[FirestoreData]
public class FireMegaLaserInteraction : InteractionData
{
    public required HexCoordinates Target { get; set; }
}