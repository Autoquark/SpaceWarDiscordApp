using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

/// <summary>
/// Global persistent bot data
/// </summary>
[FirestoreData]
public class GlobalData : FirestoreModel
{
    /// <summary>
    /// Incrementing ID associated with the InteractionData for a set of buttons which is presented to the user
    /// at the same time. Can be used by AI players to identify their current set of available choices in a game. 
    /// </summary>
    [FirestoreProperty]
    public ulong InteractionGroupId { get; set; } = 1;
}