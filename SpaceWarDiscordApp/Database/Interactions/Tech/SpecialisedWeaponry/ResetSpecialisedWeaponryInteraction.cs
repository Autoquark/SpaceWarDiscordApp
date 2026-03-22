using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents.Refresh;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.SpecialisedWeaponry;

[FirestoreData]
public class ResetSpecialisedWeaponryInteraction : EventModifyingInteractionData<GameEvent_TechRefreshed>
{
}