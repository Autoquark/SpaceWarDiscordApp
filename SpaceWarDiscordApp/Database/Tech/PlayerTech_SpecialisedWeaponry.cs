using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.Tech;

[FirestoreData]
public class PlayerTech_SpecialisedWeaponry : PlayerTech
{
    [FirestoreProperty]
    public int TargetGamePlayerId { get; set; } = -1;
}