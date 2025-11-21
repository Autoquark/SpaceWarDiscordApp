using DSharpPlus.Entities;
using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

public static class TransactionExtensions
{
    public static async Task<GuildData> GetOrCreateGuildDataAsync(this Transaction transaction, ulong guildId) =>
        (await transaction.GetSnapshotAsync(
            new Query<GuildData>(transaction.Database.GuildData())
                .WhereEqualTo(x => x.GuildId, guildId)
                .Limit(1)))
        .FirstOrDefault()
        ?.ConvertTo<GuildData>() ?? new GuildData { GuildId = guildId, DocumentId = transaction.Database.GuildData().Document() };

    public static async Task<Game?> GetGameForChannelAsync(this Transaction transaction, DiscordChannel channel)
    {
        if (channel is DiscordThreadChannel threadChannel)
        {
            channel = threadChannel.Parent;
        }
        var game = (await transaction.GetSnapshotAsync(
                new Query<Game>(transaction.Database.Games()).WhereEqualTo(x => x.GameChannelId, channel.Id)
                    .Limit(1)))
            .FirstOrDefault()
            ?.ConvertTo<Game>();

        return game;
    }

    public static async Task<Game?> GetGameAsync(this Transaction transaction, DocumentReference gameRef)
        => (await transaction.GetSnapshotAsync(gameRef)) .ConvertTo<Game>();

    public static async Task<T> GetInteractionDataAsync<T>(this Transaction transaction, Guid interactionId)
        where T : InteractionData.InteractionData
    {
        var interactionData = await GetInteractionDataAsync(transaction, interactionId);
        if (interactionData is T typedInteractionData)
        {
            return typedInteractionData;
        }

        if (interactionData == null)
        {
            throw new Exception($"Interaction data with ID {interactionId} not found");
        }
        
        throw new Exception($"Expected interaction data of type {typeof(T).FullName}, but got {interactionData.GetType().FullName}");
    }

    public static async Task<InteractionData.InteractionData?> GetInteractionDataAsync(this Transaction transaction,
        Guid interactionId) =>
        (await transaction.GetSnapshotAsync(
                new Query<InteractionData.InteractionData>(transaction.Database.InteractionData())
                    .WhereEqualTo(x => x.InteractionId, interactionId.ToString())
            .Limit(1)))
        .FirstOrDefault()
        ?.ConvertTo<InteractionData.InteractionData>();

    public static void Set(this Transaction transaction, FirestoreDocument document) => transaction.Set(document.DocumentId, document);
}