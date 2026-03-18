using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.Tech_MaterialRepurposing;

[FirestoreData]
public class UseMaterialRepurposingInteraction : EventModifyingInteractionData<GameEvent_CapturePlanet>
{
    
}