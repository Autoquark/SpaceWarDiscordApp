using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.DatabaseModels;

[FirestoreData]
public record class Planet
{
    public Planet() { }

    public Planet(Planet otherPlanet)
    {
        Production = otherPlanet.Production;
        Science = otherPlanet.Science;
        Stars = otherPlanet.Stars;
        ForcesPresent = otherPlanet.ForcesPresent;
        IsHomeSystem = otherPlanet.IsHomeSystem;
    }

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
    
    [FirestoreProperty]
    public bool IsHomeSystem { get; set; } = false;
    
    [FirestoreProperty]
    public bool IsExhausted { get; set; } = false;
    
    public bool IsNeutral => OwningPlayerId == -1;
}