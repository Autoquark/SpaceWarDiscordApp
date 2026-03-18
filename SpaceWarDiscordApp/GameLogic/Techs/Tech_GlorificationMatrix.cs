namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_GlorificationMatrix : Tech
{
    public const string StaticId = "glorification-matrix";
    
    public Tech_GlorificationMatrix() : base(StaticId,
        "Glorification Matrix",
        "This tech counts as 1 $star$",
        "This technology represents a glorious leap forward for our civilisation (p >= 0.95)!")
    {
        
    }
    
    // Effects are currently hardcoded inside GameStateOperations.GetPlayerStars
}