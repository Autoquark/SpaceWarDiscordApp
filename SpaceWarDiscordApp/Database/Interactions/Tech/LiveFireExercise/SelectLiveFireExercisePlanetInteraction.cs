using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.LiveFireExercise;

[FirestoreData]
public class SelectLiveFireExercisePlanetInteraction : InteractionData
{
    [FirestoreProperty]
    public required HexCoordinates Target { get; set; }
}
