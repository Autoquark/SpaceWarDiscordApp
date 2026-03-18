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

[FirestoreData(ConverterType = typeof(FirestoreEnumNameConverter<TechMarketRule>))]
public enum TechMarketRule
{
    /// <summary>
    /// A queue of techs with the most recently added costing more. Whenever a tech is purchased the remaining techs
    /// move along, with any tech in the rightmost slot being discarded and a new tech dealt into the leftmost slot. 
    /// </summary>
    Queue,
    /// <summary>
    /// There is one tech slot per player. At the end of a player's turn, the tech in their slot decreases in price. If
    /// a tech's price reaches zero it is discarded, the price is reset and a new tech is dealt out. Players can buy a
    /// tech from any slot. 
    /// </summary>
    DiscountingSlots
}

[FirestoreData]
public class GameRules : FirestoreDocument
{
    [FirestoreProperty]
    public StartingTechRule StartingTechRule { get; set; } = StartingTechRule.None;
    
    [FirestoreProperty]
    public ScoringRule ScoringRule { get; set; } = ScoringRule.MostStars;
    
    [FirestoreProperty]
    public TechMarketRule TechMarketRule { get; set; } = TechMarketRule.Queue;
    
    [FirestoreProperty]
    public int VictoryThreshold { get; set; } = 6;

    [FirestoreProperty]
    public string MapGeneratorId { get; set; } = DefaultMapGenerator.StaticId;
    
    [FirestoreProperty]
    public int MaxPlayers { get; set; } = GameConstants.MaxPlayerCount;

    [FirestoreProperty]
    public bool SingleUseTechCanBeUniversal { get; set; } = true;
}