using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.FungibleAnatomy;

[FirestoreData]
public class UseFungibleAnatomyInteraction : EventModifyingInteractionData<GameEvent_ExceedingPlanetCapacity>
{
}