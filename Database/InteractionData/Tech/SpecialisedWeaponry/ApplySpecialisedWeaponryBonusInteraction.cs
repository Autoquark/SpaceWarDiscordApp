using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.SpecialisedWeaponry;

[FirestoreData]
public class ApplySpecialisedWeaponryBonusInteraction : EventModifyingInteractionData<GameEvent_PreMove>
{
    [FirestoreProperty]
    public required bool IsAttacker { get; set; }
}