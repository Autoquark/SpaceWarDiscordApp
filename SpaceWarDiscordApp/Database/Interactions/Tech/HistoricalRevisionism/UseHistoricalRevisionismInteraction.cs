using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.HistoricalRevisionism;

[FirestoreData]
public class UseHistoricalRevisionismInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates FirstTarget { get; set; }
    
    [FirestoreProperty]
    public required HexCoordinates SecondTarget { get; set; }
}