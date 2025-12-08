using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.SpecialisedWeaponry;

[FirestoreData]
public class ResetSpecialisedWeaponryInteraction : EventModifyingInteractionData<GameEvent_TechRefreshed>
{
}