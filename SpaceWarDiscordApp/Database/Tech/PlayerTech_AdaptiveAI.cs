using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.Tech;

[FirestoreData]
public class PlayerTech_AdaptiveAI : PlayerTech
{
    [FirestoreProperty]
    public int Progress { get; set; } = 0;
}