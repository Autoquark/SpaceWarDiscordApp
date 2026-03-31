namespace Tumult.GameLogic;

/// <summary>
/// Scoped per-operation state used by GameEventDispatcher to prevent re-entrant event stack resolution.
/// </summary>
public class PerOperationState
{
    public bool IsResolvingStack { get; set; }
}
