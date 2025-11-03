using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.PeoplePrinters;

[FirestoreData]
public class SpecifyPeoplePrintersAmountInteraction : EventModifyingInteractionData<GameEvent_BeginProduce>
{
    [FirestoreProperty]
    public required int ScienceAmount { get; set; }
}