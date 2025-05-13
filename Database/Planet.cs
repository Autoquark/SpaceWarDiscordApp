using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

[FirestoreData]
public class Planet
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

    public void SubtractForces(int amount)
    {
        if (amount > ForcesPresent)
        {
            throw new Exception();
        }
        
        ForcesPresent -= amount;
        if (ForcesPresent == 0)
        {
            OwningPlayerId = -1;
        }
    }
}