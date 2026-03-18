using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.InteractionData.Tech.WarpNodes;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.GameEvents.Tech;

[FirestoreData]
public class GameEvent_ChooseWarpNodesDestination : GameEvent_PlayerChoice<WarpNodes_ChooseDestinationInteraction>
{
    [FirestoreProperty]
    public required int PlayerGameId { get; set; }
    
    [FirestoreProperty]
    public required HexCoordinates Source { get; set; }
}