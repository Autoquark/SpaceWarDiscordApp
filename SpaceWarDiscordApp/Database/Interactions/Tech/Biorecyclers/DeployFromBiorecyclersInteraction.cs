using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.Biorecyclers;

[FirestoreData]
public class DeployFromBiorecyclersInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates Location { get; set; }
}