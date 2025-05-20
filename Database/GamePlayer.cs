using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database;

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
    public PlayerColour PlayerColour { get; set; }
    
    [FirestoreProperty]
    public string? DummyPlayerName { get; set; }
    
    [FirestoreProperty]
    public PlannedMove? PlannedMove { get; set; }

    [FirestoreProperty]
    public bool IsEliminated { get; set; } = false;

    public bool IsDummyPlayer => DiscordUserId == 0;
    
    public PlayerColourInfo PlayerColourInfo => PlayerColourInfo.Get(PlayerColour);
}