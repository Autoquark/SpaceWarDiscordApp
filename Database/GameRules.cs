using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic.MapGeneration;

namespace SpaceWarDiscordApp.Database;

[FirestoreData(ConverterType = typeof(FirestoreEnumNameConverter<StartingTechRule>))]
public enum StartingTechRule
{
    None,
    OneUniversal,
    IndividualDraft
}

[FirestoreData]
public class GameRules : FirestoreDocument
{
    [FirestoreProperty]
    public StartingTechRule StartingTechRule { get; set; } = StartingTechRule.None;

    [FirestoreProperty]
    public string MapGeneratorId { get; set; } = DefaultMapGenerator.StaticId;
    
    [FirestoreProperty]
    public int MaxPlayers { get; set; } = 6;
}