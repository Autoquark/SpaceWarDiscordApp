using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.InteractionData.Tech.NanoconstructorSwarm;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.GameEvents.Tech;

[FirestoreData]
public class GameEvent_ChooseNanoconstructorNextPlanet : GameEvent_PlayerChoice<SelectNanoconstructorSwarmNextPlanetInteraction>
{
    [FirestoreProperty]
    public required int PlayerGameId { get; set; }
    
    [FirestoreProperty]
    public required List<HexCoordinates> RemainingPlanets { get; set; }
}