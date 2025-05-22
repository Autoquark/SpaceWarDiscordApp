namespace SpaceWarDiscordApp.GameLogic.Techs;

public class TechAction
{
    public TechAction(Tech tech)
    {
        Tech = tech;
    }
    
    public Tech Tech { get; }
    
    public required string DisplayName { get; init; }

    /// <summary>
    /// Id that can be used to distinguish multiple actions provided by the same tech
    /// </summary>
    public string Id { get; init; } = "";
    
    public bool IsAvailable { get; init; } = true;
}