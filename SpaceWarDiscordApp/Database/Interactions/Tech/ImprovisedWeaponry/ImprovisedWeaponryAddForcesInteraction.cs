using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.ImprovisedWeaponry;

[FirestoreData]
public class ImprovisedWeaponryAddForcesInteraction : EventModifyingInteractionData<GameEvent_CapturePlanet>
{
}