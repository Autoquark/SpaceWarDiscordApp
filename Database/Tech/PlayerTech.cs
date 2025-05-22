using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.Converters;

namespace SpaceWarDiscordApp.Database;

/// <summary>
/// Stores information relating to a player's ownership of a tech in a game
/// </summary>
[FirestoreData]
public class PlayerTech : PolymorphicFirestoreModel
{
    [FirestoreProperty]
    public required string TechId { get; set; }
    
    [FirestoreProperty]
    public bool IsExhausted { get; set; } = false;
}