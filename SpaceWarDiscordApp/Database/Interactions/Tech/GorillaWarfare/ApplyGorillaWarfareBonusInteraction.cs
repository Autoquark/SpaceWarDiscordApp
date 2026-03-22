using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents.Movement;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.GorillaWarfare;

[FirestoreData]
public class ApplyGorillaWarfareBonusInteraction : EventModifyingInteractionData<GameEvent_PreMove>
{
    [FirestoreProperty]
    public required bool IsAttacker { get; set; }
}