using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.SmoothTakeover;

[FirestoreData]
public class SmoothTakeoverRefreshInteraction : EventModifyingInteractionData<GameEvent_CapturePlanet>
{
    
}