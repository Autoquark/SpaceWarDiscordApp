using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.OptimisedWorkSchedules;

[FirestoreData]
public class TargetOptimisedWorkSchedulesInteraction : InteractionData
{
    [FirestoreProperty]
    public HexCoordinates Target { get; set; }
}