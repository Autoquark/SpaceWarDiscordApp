using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

[FirestoreData]
public class GuildData : FirestoreDocument
{
    [FirestoreProperty]
    public ulong GuildId { get; set; }
    
    [FirestoreProperty]
    public ulong TechListingChannelId { get; set; }

    [FirestoreProperty]
    public List<ulong> TechListingMessageIds { get; set; } = [];
}