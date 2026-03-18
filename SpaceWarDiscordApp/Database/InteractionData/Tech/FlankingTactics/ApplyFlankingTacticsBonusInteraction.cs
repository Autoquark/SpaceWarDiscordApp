using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Movement;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.FlankingTactics;

[FirestoreData]
public class ApplyFlankingTacticsBonusInteraction : EventModifyingInteractionData<GameEvent_PreMove>
{
}