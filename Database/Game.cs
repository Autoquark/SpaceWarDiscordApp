using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.DatabaseModels;

public enum GamePhase
{
    GatherPlayers,
    Setup,
    Play,
    Finished
}

[FirestoreData]
public class Game : FirestoreModel
{
    /// <summary>
    /// All players in the game. Once the game has started, they will be in turn order.
    /// </summary>
    [FirestoreProperty]
    public List<GamePlayer> Players { get; set; } = [];
    
    [FirestoreProperty]
    public string Name { get; set; } = "Untitled Game";

    [FirestoreProperty]
    public GamePhase Phase { get; set; } = GamePhase.GatherPlayers;
    
    [FirestoreProperty]
    public int TurnNumber { get; set; } = 1;

    [FirestoreProperty]
    public ulong GameChannelId { get; set; } = 0;
    
    [FirestoreProperty]
    public List<BoardHex> Hexes { get; set; } = [];
    
    [FirestoreProperty]
    public int CurrentTurnPlayerIndex { get; set; } = 0;
    
    [FirestoreProperty]
    public int ScoringTokenPlayerIndex { get; set; } = 0;
    
    public GamePlayer CurrentTurnPlayer => Players[CurrentTurnPlayerIndex];
    
    public GamePlayer ScoringTokenPlayer => Players[ScoringTokenPlayerIndex];

    public GamePlayer GetGamePlayerByGameId(int id) => Players.First(x => x.GamePlayerId == id);
}