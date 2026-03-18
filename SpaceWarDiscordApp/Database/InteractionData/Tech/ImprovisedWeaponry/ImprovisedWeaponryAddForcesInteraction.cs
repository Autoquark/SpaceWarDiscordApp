using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.ImprovisedWeaponry;

[FirestoreData]
public class ImprovisedWeaponryAddForcesInteraction : EventModifyingInteractionData<GameEvent_CapturePlanet>
{
}