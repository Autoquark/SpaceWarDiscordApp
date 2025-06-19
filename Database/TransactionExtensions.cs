using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

public static class TransactionExtensions
{
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

        foreach (var gamePlayer in game.Players)
        {
            await gamePlayer.Techs.PopulateAsync(transaction); //TODO: Fetch in parallel?
            await gamePlayer.LastTurnEvents.PopulateAsync(transaction);
            await gamePlayer.CurrentTurnEvents.PopulateAsync(transaction);
        }
        
        return game;
    }

    public static void Set(this Transaction transaction, FirestoreModel model)
    {
        if (model is Game game)
        {
            foreach (var gamePlayer in game.Players)
            {
                gamePlayer.Techs.OnSavingParentDoc(transaction);
                gamePlayer.LastTurnEvents.OnSavingParentDoc(transaction);
                gamePlayer.CurrentTurnEvents.OnSavingParentDoc(transaction);
            }
        }
        
        transaction.Set(model.DocumentId, model);
    }
}