using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.IntensiveTraining;

[FirestoreData]
public class ApplyIntensiveTrainingBonusInteraction : EventModifyingInteractionData<GameEvent_PreMove>
{
    [FirestoreProperty]
    public bool IsAttacker { get; set; }
}