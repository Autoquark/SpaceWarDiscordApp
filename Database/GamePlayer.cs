using Google.Cloud.Firestore;
using SixLabors.ImageSharp;

namespace SpaceWarDiscordApp.DatabaseModels;

[FirestoreData]
public class GamePlayer
{
    [FirestoreProperty]
    public ulong DiscordUserId { get; set; } = 0;
    
    [FirestoreProperty]
    public int GamePlayerId { get; set; } = -1;
    
    [FirestoreProperty]
    public int VictoryPoints { get; set; } = 0;
    
    [FirestoreProperty]
    public int Science { get; set; } = 0;
    
    [FirestoreProperty]
    public Color PlayerColor { get; set; }
}