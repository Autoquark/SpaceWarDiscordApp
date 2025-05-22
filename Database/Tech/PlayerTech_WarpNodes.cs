using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.Database.Tech;

[FirestoreData]
public class PlayerTech_WarpNodes : PlayerTech
{
    [FirestoreProperty]
    public List<HexCoordinates> MovedTo { get; set; } = [];
    
    [FirestoreProperty]
    public HexCoordinates Source { get; set; }
}