using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.InteractionData.Tech;

namespace SpaceWarDiscordApp.Database.GameEvents.Setup;

[FirestoreData]
public class GameEvent_PlayersChooseStartingTech : GameEvent_PlayerChoice<ChoosePlayerStartingTechInteraction>
{
}