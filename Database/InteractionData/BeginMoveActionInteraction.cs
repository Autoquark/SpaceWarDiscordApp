using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData;

[FirestoreData]
public class BeginMoveActionInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates Destination { get; set; }
    
    [FirestoreProperty]
    public int MovingGamePlayerId { get; set; }
}