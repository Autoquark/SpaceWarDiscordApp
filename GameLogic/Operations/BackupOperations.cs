using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public static class BackupOperations
{
    public static async Task SaveBackupAsync(Game game)
    {
        var originalRef = game.DocumentId;
        game.DocumentId = Program.FirestoreDb.GameBackups().Document();
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));
    }
}