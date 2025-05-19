using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData;

[FirestoreData]
public class ShowProduceOptionsInteraction : InteractionData
{
    [FirestoreProperty]
    public required int ForPlayerGameId { get; set; }
}