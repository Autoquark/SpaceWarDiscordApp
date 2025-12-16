using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.MapGeneration;

namespace SpaceWarDiscordApp.Database;

[FirestoreData(ConverterType = typeof(FirestoreEnumNameConverter<StartingTechRule>))]
public enum StartingTechRule
{
    None,
    OneUniversal,
    IndividualDraft
}

[FirestoreData(ConverterType = typeof(FirestoreEnumNameConverter<ScoringRule>))]
public enum ScoringRule
{
    /// <summary>
    /// When a player's turn ends, if they have the scoring token, scoring is checked. If any player has more stars than all others, they score a point
    /// </summary>
    MostStars,
    /// <summary>
    /// At the end of each player's turn they score one VP per star they currently control. First to the goal number of VP wins.
    /// </summary>
    Cumulative
}

[FirestoreData]
public class GameRules : FirestoreDocument
{
    [FirestoreProperty]
    public StartingTechRule StartingTechRule { get; set; } = StartingTechRule.None;
    
    [FirestoreProperty]
    public ScoringRule ScoringRule { get; set; } = ScoringRule.MostStars;
    
    [FirestoreProperty]
    public int VictoryThreshold { get; set; } = 6;

    [FirestoreProperty]
    public string MapGeneratorId { get; set; } = DefaultMapGenerator.StaticId;
    
    [FirestoreProperty]
    public int MaxPlayers { get; set; } = GameConstants.MaxPlayerCount;
}