using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.MassMigration;

[FirestoreData]
public class ApplyMassMigrationBonusInteraction : EventModifyingInteractionData<GameEvent_PreMove>
{
    
}