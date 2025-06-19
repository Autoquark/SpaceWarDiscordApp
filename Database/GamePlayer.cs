using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.ActionRecords;
using SpaceWarDiscordApp.Database.Converters;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database;

[FirestoreData]
public class GamePlayer
{
    public GamePlayer()
    {
        Techs = new LinkedDocumentCollection<PlayerTech>(Program.FirestoreDb.PlayerTechs(), () => TechsDocuments);
        LastTurnActions = new LinkedDocumentCollection<ActionRecord>(Program.FirestoreDb.ActionRecords(), () => LastTurnActionsDocuments);
        CurrentTurnActions = new LinkedDocumentCollection<ActionRecord>(Program.FirestoreDb.ActionRecords(), () => CurrentTurnActionsDocuments);
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
    private IList<DocumentReference> LastTurnActionsDocuments { get; set; } = [];
    
    public LinkedDocumentCollection<ActionRecord> LastTurnActions { get; }
    
    [FirestoreProperty]
    private IList<DocumentReference> CurrentTurnActionsDocuments { get; set; } = [];
    public LinkedDocumentCollection<ActionRecord> CurrentTurnActions { get; }

    public bool IsDummyPlayer => DiscordUserId == 0;
    
    public PlayerColourInfo PlayerColourInfo => PlayerColourInfo.Get(PlayerColour);

    public PlayerTech GetPlayerTechById(string techId) => Techs.First(x => x.TechId == techId);
    public PlayerTech? TryGetPlayerTechById(string techId) => Techs.FirstOrDefault(x => x.TechId == techId);
    public T GetPlayerTechById<T>(string techId) where T : PlayerTech => (T)Techs.First(x => x.TechId == techId);
}