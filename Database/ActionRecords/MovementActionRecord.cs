using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.ActionRecords;

[FirestoreData]
public class MovementActionRecord : ActionRecord
{
    [FirestoreProperty]
    public IList<SourceAndAmount> Sources { get; set; }
    
    [FirestoreProperty]
    public HexCoordinates Destination { get; set; }
}