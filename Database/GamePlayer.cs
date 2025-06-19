using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.Converters;
using SpaceWarDiscordApp.Database.EventRecords;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database;

[FirestoreData]
public class GamePlayer
{
    public GamePlayer()
    {
        Techs = new LinkedDocumentCollection<PlayerTech>(Program.FirestoreDb.PlayerTechs(), () => TechsDocuments);
        LastTurnEvents = new LinkedDocumentCollection<EventRecord>(Program.FirestoreDb.ActionRecords(), () => LastTurnEventsDocuments);
        CurrentTurnEvents = new LinkedDocumentCollection<EventRecord>(Program.FirestoreDb.ActionRecords(), () => CurrentTurnEventsDocuments);
    }
    
    [FirestoreProperty]
    public ulong DiscordUserId { get; set; }
    
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
    private IList<DocumentReference> TechsDocuments { get; set; } = [];

    /// <summary>
    /// PlayerTech objects for techs owned by this player
    /// </summary>
    /// <remarks>NOT a FirestoreProperty, we manually populate this from the subcollection when querying a game</remarks>
    public LinkedDocumentCollection<PlayerTech> Techs { get; }
    
    [FirestoreProperty]
    private IList<DocumentReference> LastTurnEventsDocuments { get; set; } = [];
    
    /// <summary>
    /// Events associated with this player from their last turn. Used to draw recap icons on the map
    /// </summary>
    public LinkedDocumentCollection<EventRecord> LastTurnEvents { get; }
    
    [FirestoreProperty]
    private IList<DocumentReference> CurrentTurnEventsDocuments { get; set; } = [];
    
    /// <summary>
    /// Events associated with this player from their current turn. Will become LastTurnEvents when the turn ends
    /// </summary>
    public LinkedDocumentCollection<EventRecord> CurrentTurnEvents { get; }

    public bool IsDummyPlayer => DiscordUserId == 0;
    
    public PlayerColourInfo PlayerColourInfo => PlayerColourInfo.Get(PlayerColour);

    public PlayerTech GetPlayerTechById(string techId) => Techs.First(x => x.TechId == techId);
    public PlayerTech? TryGetPlayerTechById(string techId) => Techs.FirstOrDefault(x => x.TechId == techId);
    public T GetPlayerTechById<T>(string techId) where T : PlayerTech => (T)Techs.First(x => x.TechId == techId);
}