using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.VolunteerTesters;

[FirestoreData]
public class UseVolunteerTestersInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates Target { get; set; }
    
    [FirestoreProperty]
    public required int Amount { get; set; }
}