using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic.MapGeneration;

namespace SpaceWarDiscordApp.Database;

[FirestoreData(ConverterType = typeof(FirestoreEnumNameConverter<StartingTechRule>))]
public enum StartingTechRule
{
    None,
    OneUniversal
}

[FirestoreData]
public class GameRules : FirestoreModel
{
    public StartingTechRule StartingTechRule { get; set; } = StartingTechRule.None;

    public string MapGeneratorId { get; set; } = DefaultMapGenerator.StaticId;
}