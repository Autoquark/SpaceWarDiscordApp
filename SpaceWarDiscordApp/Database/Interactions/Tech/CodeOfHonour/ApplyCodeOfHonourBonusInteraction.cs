using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents.Movement;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.CodeOfHonour;

[FirestoreData]
public class ApplyCodeOfHonourBonusInteraction : EventModifyingInteractionData<GameEvent_PreMove>
{
    [FirestoreProperty]
    public required bool IsAttacker { get; set; }
}