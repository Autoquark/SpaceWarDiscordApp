using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents.Movement;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.EnPassant;

[FirestoreData]
public class ResolveEnPassantInteraction : EventModifyingInteractionData<GameEvent_MovementFlowComplete>
{
}