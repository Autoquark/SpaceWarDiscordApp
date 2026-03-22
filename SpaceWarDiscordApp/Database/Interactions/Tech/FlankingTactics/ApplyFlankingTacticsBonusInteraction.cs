using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents.Movement;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.FlankingTactics;

[FirestoreData]
public class ApplyFlankingTacticsBonusInteraction : EventModifyingInteractionData<GameEvent_PreMove>
{
}