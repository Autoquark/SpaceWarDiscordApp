using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.SmoothTakeover;

[FirestoreData]
public class SmoothTakeoverRefreshInteraction : EventModifyingInteractionData<GameEvent_CapturePlanet>
{
    
}