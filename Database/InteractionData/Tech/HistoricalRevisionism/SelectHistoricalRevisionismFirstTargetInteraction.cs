using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.HistoricalRevisionism;

[FirestoreData]
public class SelectHistoricalRevisionismFirstTargetInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates Target { get; set; }
}