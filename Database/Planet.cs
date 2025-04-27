using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.DatabaseModels;

[FirestoreData]
public class Planet
{
    [FirestoreProperty]
    public int Production {get; set;} = 0;
    
    [FirestoreProperty]
    public int Science {get; set;} = 0;
    
    [FirestoreProperty]
    public int Stars {get; set;} = 0;
    
    [FirestoreProperty]
    public int ForcesPresent {get; set;} = 0;

    [FirestoreProperty]
    public int OwningPlayerId { get; set; } = -1;
    
    public bool IsNeutral => OwningPlayerId == -1;
}