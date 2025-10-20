using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.RousingSpeech;

[FirestoreData]
public class ApplyRousingSpeechBonusInteraction : EventModifyingInteractionData<GameEvent_PreMove>
{
    [FirestoreProperty]
    public bool IsAttacker { get; set; }
    
}