using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

[FirestoreData]
public class GuildData : FirestoreModel
{
    [FirestoreProperty]
    public ulong GuildId { get; set; }
    
    [FirestoreProperty]
    public ulong TechListingChannelId { get; set; }

    [FirestoreProperty]
    public List<ulong> TechListingMessageIds { get; set; } = [];
}