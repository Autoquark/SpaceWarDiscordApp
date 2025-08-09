using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.CodeOfHonour;

[FirestoreData]
public class ApplyCodeOfHonourBonusInteraction : EventModifyingInteractionData<GameEvent_PreMove>
{
    [FirestoreProperty]
    public required bool IsAttacker { get; set; }
}