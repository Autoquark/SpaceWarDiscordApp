using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.Tech;

[FirestoreData]
public class PlayerTech_Biorecyclers : PlayerTech
{
    [FirestoreProperty]
    public int Forces { get; set; } = 0;
}