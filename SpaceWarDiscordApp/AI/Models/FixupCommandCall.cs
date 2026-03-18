using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.AI.Models;

/// <summary>
/// Represents a parsed AI function call that maps to a FixupCommands method
/// </summary>
public abstract class FixupCommandCall
{
    public string FunctionName { get; set; } = "";
}

public class SetForcesCall : FixupCommandCall
{
    public HexCoordinates Coordinates { get; set; }
    public int Amount { get; set; } = -1;
    public int Player { get; set; } = -1;

    public SetForcesCall()
    {
        FunctionName = "setForces";
    }
}

public class GrantTechCall : FixupCommandCall
{
    public string TechId { get; set; } = "";
    public int Player { get; set; } = -1;

    public GrantTechCall()
    {
        FunctionName = "grantTech";
    }
}

public class RemoveTechCall : FixupCommandCall
{
    public string TechId { get; set; } = "";
    public int Player { get; set; } = -1;

    public RemoveTechCall()
    {
        FunctionName = "removeTech";
    }
}

public class SetTechExhaustedCall : FixupCommandCall
{
    public string TechId { get; set; } = "";
    public int Player { get; set; } = -1;
    public bool Exhausted { get; set; } = true;

    public SetTechExhaustedCall()
    {
        FunctionName = "setTechExhausted";
    }
}

public class SetPlanetExhaustedCall : FixupCommandCall
{
    public HexCoordinates Coordinates { get; set; }
    public bool Exhausted { get; set; } = true;

    public SetPlanetExhaustedCall()
    {
        FunctionName = "setPlanetExhausted";
    }
}

public class SetPlayerTurnCall : FixupCommandCall
{
    public int Player { get; set; } = -1;

    public SetPlayerTurnCall()
    {
        FunctionName = "setPlayerTurn";
    }
}

public class SetPlayerScienceCall : FixupCommandCall
{
    public int Science { get; set; }
    public int Player { get; set; } = -1;

    public SetPlayerScienceCall()
    {
        FunctionName = "setPlayerScience";
    }
}

public class SetPlayerVictoryPointsCall : FixupCommandCall
{
    public int Vp { get; set; }
    public int Player { get; set; } = -1;

    public SetPlayerVictoryPointsCall()
    {
        FunctionName = "setPlayerVictoryPoints";
    }
} 