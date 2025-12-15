using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public class ProdOperations
{
    public static void UpdateProdTimers(Game game, NonDbGameState nonDbGameState)
    {
        if (game.Phase != GamePhase.Play)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var delay = GameConstants.TurnProdInterval - (now - game.LastTurnProdTime);

        if (delay < TimeSpan.Zero)
        {
            delay = TimeSpan.Zero;
        }
        
        nonDbGameState.TurnProdTimer.Cancel();
        nonDbGameState.TurnProdTimer = new CancellationTokenSource();
        _ = SendTurnProdMessageAfterDelay(game.DocumentId!, delay, nonDbGameState.TurnProdTimer.Token);

        nonDbGameState.UnfinishedTurnProdTimer.Cancel();

        // If an action has been taken this turn, and enough time passes without another action being taken or the turn being
        // ended, send a reminder to end the turn
        if (game.ActionTakenThisTurn)
        {
            delay = GameConstants.UnfinishedTurnProdTime - (now - game.LatestUnfinishedTurnProdTime);
            if (delay < TimeSpan.Zero)
            {
                delay = TimeSpan.Zero;
            }
            
            nonDbGameState.UnfinishedTurnProdTimer = new CancellationTokenSource();
            _ = SendUnfinishedTurnPropMessageAfterDelay(game.DocumentId!, delay, nonDbGameState.UnfinishedTurnProdTimer.Token);
        }
    }

    private static async Task SendTurnProdMessageAfterDelay(DocumentReference gameDoc, TimeSpan delay, CancellationToken cancellationToken)
    {
        await Task.Delay(delay, cancellationToken);
        
        using var serviceScope = Program.DiscordClient.ServiceProvider.CreateScope();
        if (!serviceScope.ServiceProvider.GetRequiredService<GameCache>().GetGame(gameDoc, out var game, out var nonDbGameState))
        {
            throw new Exception("Dangling prod timer for game that is not in memory cache");
        }

        using var disposable = serviceScope.ServiceProvider.GetRequiredService<GameSyncManager>().Locker
            .LockOrNull(gameDoc, 0, cancellationToken);

        if (disposable != null)
        {
            var player = game.CurrentTurnPlayer;
            var channel = await Program.DiscordClient.TryGetChannelAsync(game.GameChannelId);
            if (channel! == null!)
            {
                return;
            }
            
            await channel.SendMessageAsync($"{await player.GetNameAsync(true)}, polite reminder that it's your turn");
            game.LastTurnProdTime = DateTime.UtcNow;

            // Don't forward cancellation token, at the point we've sent the reminder we want to record that we did it
            await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game), cancellationToken: CancellationToken.None);
            
            _ = SendTurnProdMessageAfterDelay(gameDoc, GameConstants.TurnProdInterval, cancellationToken);
        }
        else
        {
            _ = SendTurnProdMessageAfterDelay(gameDoc, TimeSpan.FromSeconds(10), cancellationToken);
        }
    }

    private static async Task SendUnfinishedTurnPropMessageAfterDelay(DocumentReference gameDoc, TimeSpan delay,
        CancellationToken cancellationToken)
    {
        await Task.Delay(delay, cancellationToken);
        
        using var serviceScope = Program.DiscordClient.ServiceProvider.CreateScope();
        if (!serviceScope.ServiceProvider.GetRequiredService<GameCache>().GetGame(gameDoc, out var game, out _))
        {
            throw new Exception("Dangling prod timer for game that is not in memory cache");
        }
        
        using var disposable = serviceScope.ServiceProvider.GetRequiredService<GameSyncManager>().Locker
            .LockOrNull(gameDoc, 0, cancellationToken);
        
        if (disposable != null)
        {
            var player = game.CurrentTurnPlayer;
            var channel = await Program.DiscordClient.TryGetChannelAsync(game.GameChannelId);
            if (channel! == null!)
            {
                return;
            }
            
            await channel.SendMessageAsync($"{await player.GetNameAsync(true)}, sorry to bother you but did you perhaps forget to end your turn?");
            game.LatestUnfinishedTurnProdTime = DateTime.UtcNow;
            
            // Don't forward cancellation token, at the point we've sent the reminder we want to record that we did it
            await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game), cancellationToken: CancellationToken.None);
            
            // Don't repeat this prod message until the next turn
        }
        else
        {
            _ = SendUnfinishedTurnPropMessageAfterDelay(gameDoc, TimeSpan.FromSeconds(10), cancellationToken);
        }
    }
}