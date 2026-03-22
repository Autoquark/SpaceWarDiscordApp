using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.Tech;

[FirestoreData]
public class PlayerTech_WarpNodes : PlayerTech
{
    [FirestoreProperty]
    public List<HexCoordinates> MovedTo { get; set; } = [];
    
    [FirestoreProperty]
    public HexCoordinates Source { get; set; }
}