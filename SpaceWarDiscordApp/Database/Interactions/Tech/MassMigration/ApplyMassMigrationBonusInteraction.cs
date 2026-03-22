using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents.Movement;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.MassMigration;

[FirestoreData]
public class ApplyMassMigrationBonusInteraction : EventModifyingInteractionData<GameEvent_PreMove>
{
    
}