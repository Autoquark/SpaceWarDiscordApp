using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.AggressiveWasteDisposal;

[FirestoreData]
public class UseAggressiveWasteDisposalInteraction : InteractionData
{
    [FirestoreProperty]
    public HexCoordinates Target { get; set; }
}