using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

[FirestoreData]
public class StartingTechHand
{
    public required List<string> Techs { get; set; } = [];
}