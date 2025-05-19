using System.Diagnostics.CodeAnalysis;
using Google.Cloud.Firestore;
using SixLabors.ImageSharp;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.GameLogic;

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
    public PlayerColour PlayerColor { get; set; }
    
    [FirestoreProperty]
    public string? DummyPlayerName { get; set; }
    
    [FirestoreProperty]
    public PlannedMove? PlannedMove { get; set; }

    public bool IsDummyPlayer => DiscordUserId == 0;
    
    public PlayerColourInfo PlayerColourInfo => PlayerColourInfo.Get(PlayerColor);
}