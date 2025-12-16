using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Refresh;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.SpecialisedWeaponry;

[FirestoreData]
public class ResetSpecialisedWeaponryInteraction : EventModifyingInteractionData<GameEvent_TechRefreshed>
{
}