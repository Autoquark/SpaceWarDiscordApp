namespace SpaceWarDiscordApp.GameLogic;

/// <summary>
/// Game related state that is not stored to the database but only lasts for the duration of one bot operation
/// </summary>
public class SpaceWarPerOperationState : PerOperationState
{
    public bool HavePrintedSelectActionMessage { get; set; } = false;
}