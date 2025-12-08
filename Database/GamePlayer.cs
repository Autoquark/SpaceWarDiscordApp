using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.Converters;
using SpaceWarDiscordApp.Database.EventRecords;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database;

[FirestoreData]
public class GamePlayer
{
    public const int GamePlayerIdNone = -1;
    
    [FirestoreProperty]
    public ulong DiscordUserId { get; set; }

    [FirestoreProperty]
    public ulong PrivateThreadId { get; set; } = 0;
    
    [FirestoreProperty]
    public int GamePlayerId { get; set; } = -1;
    
    [FirestoreProperty]
    public int VictoryPoints { get; set; }

    [FirestoreProperty]
    public int Science { get; set; }
    
    [FirestoreProperty]
    public PlayerColour PlayerColour { get; set; }
    
    [FirestoreProperty]
    public string? DummyPlayerName { get; set; }
    
    [FirestoreProperty]
    public PlannedMove? PlannedMove { get; set; }

    [FirestoreProperty]
    public bool IsEliminated { get; set; }
    
    [FirestoreProperty]
    public List<string> StartingTechs { get; set; } = [];
    
    /// <summary>
    /// Index into Game.StartingTechHands of the 'hand' of possible starting techs this player is currently choosing from
    /// </summary>
    [FirestoreProperty]
    public int CurrentStartingTechHandIndex { get; set; } = 0;

    /// <summary>
    /// PlayerTech objects for techs owned by this player
    /// </summary>
    [FirestoreProperty]
    public List<PlayerTech> Techs { get; set; } = [];

    /// <summary>
    /// Events associated with this player from their last turn. Used to draw recap icons on the map
    /// </summary>
    [FirestoreProperty]
    public List<EventRecord> LastTurnEvents { get; set; } = [];

    /// <summary>
    /// Events associated with this player from their current turn. Will become LastTurnEvents when the turn ends
    /// </summary>
    [FirestoreProperty]
    public List<EventRecord> CurrentTurnEvents { get; set; } = [];

    public bool IsDummyPlayer => DiscordUserId == 0;
    
    public PlayerColourInfo PlayerColourInfo => PlayerColourInfo.Get(PlayerColour);

    public PlayerTech GetPlayerTechById(string techId) => Techs.First(x => x.TechId == techId);
    public PlayerTech? TryGetPlayerTechById(string techId) => Techs.FirstOrDefault(x => x.TechId == techId);
    public T GetPlayerTechById<T>(string techId) where T : PlayerTech => (T)Techs.First(x => x.TechId == techId);
    public T? TryGetPlayerTechById<T>(string techId) where T : PlayerTech => (T?)Techs.FirstOrDefault(x => x.TechId == techId);
}