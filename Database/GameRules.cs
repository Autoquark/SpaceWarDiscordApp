using Google.Cloud.Firestore;

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
}