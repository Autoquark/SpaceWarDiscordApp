using System.Diagnostics.CodeAnalysis;
using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database;

public enum GamePhase
{
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
    public GamePhase Phase { get; set; } = GamePhase.Setup;
    
    [FirestoreProperty]
    public int TurnNumber { get; set; } = 1;

    [FirestoreProperty]
    public ulong GameChannelId { get; set; } = 0;
    
    [FirestoreProperty]
    public List<BoardHex> Hexes { get; set; } = [];
    
    /// <summary>
    /// Index into the Players list of the player whose turn it is. This is NOT a GamePlayerId.
    /// </summary>
    [FirestoreProperty]
    public int CurrentTurnPlayerIndex { get; set; } = 0;
    
    [FirestoreProperty]
    public int ScoringTokenPlayerIndex { get; set; } = 0;

    [FirestoreProperty]
    public List<string> UniversalTechs { get; set; } = [];
    
    [FirestoreProperty]
    public List<string> MarketTechs { get; set; } = [];
    
    public GamePlayer CurrentTurnPlayer => Players[CurrentTurnPlayerIndex];
    
    public GamePlayer ScoringTokenPlayer => Players[ScoringTokenPlayerIndex];
    public bool IsScoringTurn => CurrentTurnPlayerIndex == ScoringTokenPlayerIndex;

    public GamePlayer GetGamePlayerByGameId(int id) => Players.First(x => x.GamePlayerId == id);
    public GamePlayer? TryGetGamePlayerByGameId(int id) => Players.FirstOrDefault(x => x.GamePlayerId == id);

    public GamePlayer? GetGamePlayerByDiscordId(ulong id) => Players.FirstOrDefault(x => x.DiscordUserId == id);
    
    public BoardHex? TryGetHexAt(HexCoordinates coordinates) => Hexes.FirstOrDefault(x => x.Coordinates == coordinates);
    public BoardHex GetHexAt(HexCoordinates coordinates) => Hexes.First(x => x.Coordinates == coordinates);
}