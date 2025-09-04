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

    public static async Task<Game?> GetGameForChannelAsync(this Transaction transaction, ulong channelId)
    {
        var game = (await transaction.GetSnapshotAsync(
                new Query<Game>(transaction.Database.Games()).WhereEqualTo(x => x.GameChannelId, channelId)
                    .Limit(1)))
            .FirstOrDefault()
            ?.ConvertTo<Game>();

        if (game == null)
        {
            return null;
        }
        
        await PopulateGameSubcollectionsAsync(transaction, game);
        
        return game;
    }

    public static async Task<Game?> GetGameAsync(this Transaction transaction, DocumentReference gameRef)
    {
        var game = (await transaction.GetSnapshotAsync(gameRef))
            .ConvertTo<Game>();
        
        await PopulateGameSubcollectionsAsync(transaction, game);

        return game;
    }

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
        ?.ConvertToPolymorphic<InteractionData.InteractionData>();

    public static void Set(this Transaction transaction, FirestoreModel model)
    {
        if (model is Game game)
        {
            game.EventStack.OnSavingParentDoc(transaction);
            
            foreach (var gamePlayer in game.Players)
            {
                gamePlayer.Techs.OnSavingParentDoc(transaction);
                gamePlayer.LastTurnEvents.OnSavingParentDoc(transaction);
                gamePlayer.CurrentTurnEvents.OnSavingParentDoc(transaction);
            }
        }
        
        transaction.Set(model.DocumentId, model);
    }

    private static async Task PopulateGameSubcollectionsAsync(Transaction transaction, Game game)
    {
        await game.EventStack.PopulateAsync(transaction);

        foreach (var gamePlayer in game.Players)
        {
            await gamePlayer.Techs.PopulateAsync(transaction); //TODO: Fetch in parallel?
            await gamePlayer.LastTurnEvents.PopulateAsync(transaction);
            await gamePlayer.CurrentTurnEvents.PopulateAsync(transaction);
        }
    }
}