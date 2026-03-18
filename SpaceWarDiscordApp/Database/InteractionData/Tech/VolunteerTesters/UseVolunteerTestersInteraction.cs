using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.VolunteerTesters;

[FirestoreData]
public class UseVolunteerTestersInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates Target { get; set; }
    
    [FirestoreProperty]
    public required int Amount { get; set; }
}