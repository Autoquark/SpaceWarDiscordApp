using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.DatabaseModels;

public enum GamePhase
{
    GatherPlayers,
    Setup,
    Play,
    Finished
}

public class Game : FirestoreModel
{
    [FirestoreProperty]
    public List<ulong> DiscordUserIds { get; set; } = [];

    [FirestoreProperty]
    public GamePhase Phase { get; set; } = GamePhase.GatherPlayers;
    
    [FirestoreProperty]
    public int TurnNumber { get; set; } = 1;
}