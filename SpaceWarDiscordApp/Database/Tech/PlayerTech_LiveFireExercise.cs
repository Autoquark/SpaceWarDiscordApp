using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.Tech;

[FirestoreData]
public class PlayerTech_LiveFireExercise : PlayerTech
{
    [FirestoreProperty]
    public int TurnsActiveRemaining { get; set; } = 0;
}
