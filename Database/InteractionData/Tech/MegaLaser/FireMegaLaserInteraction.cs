using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.MegaLaser;

[FirestoreData]
public class FireMegaLaserInteraction : InteractionData
{
    public HexCoordinates Target { get; set; }
}