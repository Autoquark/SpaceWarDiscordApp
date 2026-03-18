using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.Persuadatron;

[FirestoreData]
public class UsePersuadatronInteraction: InteractionData
{
    [FirestoreProperty]
    public HexCoordinates Target { get; set; }

}