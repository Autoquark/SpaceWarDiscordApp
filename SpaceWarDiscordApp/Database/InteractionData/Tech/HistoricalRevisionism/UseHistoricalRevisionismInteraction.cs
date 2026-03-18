using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.HistoricalRevisionism;

[FirestoreData]
public class UseHistoricalRevisionismInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates FirstTarget { get; set; }
    
    [FirestoreProperty]
    public required HexCoordinates SecondTarget { get; set; }
}