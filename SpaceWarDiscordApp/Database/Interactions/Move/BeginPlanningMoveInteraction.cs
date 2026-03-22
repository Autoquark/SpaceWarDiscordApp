using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.Interactions.Move;

/// <summary>
/// Begins planning a move via the PlanMovementFlowHandler for the given type
/// </summary>
/// <typeparam name="T">Type uniquely identifying the PlanMovementFlowHandler to handle the flow</typeparam>
[FirestoreData]
public class BeginPlanningMoveInteraction<T> : InteractionData
{
    
}