namespace SpaceWarDiscordApp.GameLogic;

/// <summary>
/// Game state that is not stored in the DB but is cached in memory along with the game e.g. timers for prod messages
/// </summary>
public class NonDbGameState
{
    public CancellationTokenSource TurnProdTimer { get; set; } = new CancellationTokenSource();

    public CancellationTokenSource UnfinishedTurnProdTimer { get; set; } = new CancellationTokenSource();
}