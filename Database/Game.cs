using System.Diagnostics.CodeAnalysis;
using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database;

public enum GamePhase
{
    Setup,
    Play,
    Finished
}

[FirestoreData]
public class Game : FirestoreDocument
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
    public ulong PinnedTechMessageId { get; set; } = 0;
    
    [FirestoreProperty]
    public ulong ChatThreadId { get; set; } = 0;
    
    [FirestoreProperty]
    public List<BoardHex> Hexes { get; set; } = [];
    
    /// <summary>
    /// Index into the Players list of the player whose turn it is. This is NOT a GamePlayerId.
    /// </summary>
    [FirestoreProperty]
    public int CurrentTurnPlayerIndex { get; set; } = 0;
    
    [FirestoreProperty]
    public int ScoringTokenPlayerIndex { get; set; } = 0;

    /// <summary>
    /// Deck of techs which will be used to populate the market. Slot 0 is the 'top' from which cards are drawn.
    /// </summary>
    [FirestoreProperty]
    public List<string> TechDeck { get; set; } = [];
    
    /// <summary>
    /// Discarded tech cards. Slot 0 is the most recently discarded card.
    /// </summary>
    [FirestoreProperty]
    public List<string> TechDiscards { get; set; } = [];

    [FirestoreProperty]
    public List<StartingTechHand> StartingTechHands { get; set; } = [];

    [FirestoreProperty]
    public List<string> UniversalTechs { get; set; } = [];

    [FirestoreProperty]
    public ulong SetupMessageId { get; set; } = 0;
    
    [FirestoreProperty]
    public GameRules Rules { get; set; } = new();
    
    /// <summary>
    /// Market techs, with the first being the most expensive.
    /// </summary>
    [FirestoreProperty]
    public List<string?> TechMarket { get; set; } = [];
    
    /// <summary>
    /// Whether the current player has taken any action (main or free) this turn
    /// </summary>
    [FirestoreProperty]
    public bool AnyActionTakenThisTurn { get; set; }
    
    /// <summary>
    /// Whether the current player has taken their main action for this turn
    /// </summary>
    [FirestoreProperty]
    public bool ActionTakenThisTurn { get; set; }
    
    // ID of the last action selection message. Used to edit the buttons away when another one is posted, to avoid
    // clicking old buttons
    // Not implemented yet
    [FirestoreProperty]
    public ulong LastSelectActionMessageId { get; set; }

    /// <summary>
    /// Stack of events currently being resolved. The last event in the list is on top of the stack.
    /// </summary>
    [FirestoreProperty]
    public List<GameEvent> EventStack { get; set; } = [];
    
    /// <summary>
    /// List of saved states for this game that we can roll back to. Latest state is last.
    /// </summary>
    [FirestoreProperty]
    public List<RollbackState> RollbackStates { get; set; } = [];
    
    public GamePlayer CurrentTurnPlayer => Players[CurrentTurnPlayerIndex];
    
    public GamePlayer ScoringTokenPlayer => Players[ScoringTokenPlayerIndex];
    public bool IsScoringTurn => Players.Count == 2 || CurrentTurnPlayerIndex == ScoringTokenPlayerIndex;

    public GamePlayer GetGamePlayerByGameId(int id) => Players.First(x => x.GamePlayerId == id);
    public GamePlayer? TryGetGamePlayerByGameId(int id) => Players.FirstOrDefault(x => x.GamePlayerId == id);

    public GamePlayer GetGamePlayerByDiscordId(ulong id) => Players.First(x => x.DiscordUserId == id);
    public GamePlayer? TryGetGamePlayerByDiscordId(ulong id) => Players.FirstOrDefault(x => x.DiscordUserId == id);
    
    public GamePlayer GetGamePlayerForInteraction(InteractionData.InteractionData interaction) => GetGamePlayerByGameId(interaction.ForGamePlayerId);
    
    public BoardHex? TryGetHexAt(HexCoordinates coordinates) => Hexes.FirstOrDefault(x => x.Coordinates == coordinates);
    public BoardHex GetHexAt(HexCoordinates coordinates) => Hexes.First(x => x.Coordinates == coordinates);
    
    public IEnumerable<GamePlayer> PlayersInTurnOrderFrom(GamePlayer player) => Players.Skip(Players.IndexOf(player)).Concat(Players.Take(Players.IndexOf(player)));
}