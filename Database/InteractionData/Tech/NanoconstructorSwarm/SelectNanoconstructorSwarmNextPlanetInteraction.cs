using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.NanoconstructorSwarm;

[FirestoreData]
public class SelectNanoconstructorSwarmNextPlanetInteraction : InteractionData
{
    /// <summary>
    /// Null means quick resolve remaining planets
    /// </summary>
    [FirestoreProperty]
    public required HexCoordinates? Target { get; set; }
}