using System.Text.Json.Serialization;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.AI.Models;

public abstract class ToolCallParameters
{
    public abstract string ToolName { get; }
}

public class SetForcesParameters : ToolCallParameters
{
    public override string ToolName => "setForces";

    [JsonPropertyName("coordinates")]
    public string Coordinates { get; set; } = "";

    [JsonPropertyName("amount")]
    public int Amount { get; set; } = -1;

    [JsonPropertyName("player")]
    public int Player { get; set; } = -1;

    public HexCoordinates GetHexCoordinates()
    {
        return HexCoordinates.Parse(Coordinates);
    }
}

public class GrantTechParameters : ToolCallParameters
{
    public override string ToolName => "grantTech";

    [JsonPropertyName("techId")]
    public string TechId { get; set; } = "";

    [JsonPropertyName("player")]
    public int Player { get; set; } = -1;
}

public class RemoveTechParameters : ToolCallParameters
{
    public override string ToolName => "removeTech";

    [JsonPropertyName("techId")]
    public string TechId { get; set; } = "";

    [JsonPropertyName("player")]
    public int Player { get; set; } = -1;
}

public class SetTechExhaustedParameters : ToolCallParameters
{
    public override string ToolName => "setTechExhausted";

    [JsonPropertyName("techId")]
    public string TechId { get; set; } = "";

    [JsonPropertyName("player")]
    public int Player { get; set; } = -1;

    [JsonPropertyName("exhausted")]
    public bool Exhausted { get; set; } = true;
}

public class SetPlanetExhaustedParameters : ToolCallParameters
{
    public override string ToolName => "setPlanetExhausted";

    [JsonPropertyName("coordinates")]
    public string Coordinates { get; set; } = "";

    [JsonPropertyName("exhausted")]
    public bool Exhausted { get; set; } = true;

    public HexCoordinates GetHexCoordinates()
    {
        return HexCoordinates.Parse(Coordinates);
    }
}

public class SetPlayerTurnParameters : ToolCallParameters
{
    public override string ToolName => "setPlayerTurn";

    [JsonPropertyName("player")]
    public int Player { get; set; } = -1;
}

public class SetPlayerScienceParameters : ToolCallParameters
{
    public override string ToolName => "setPlayerScience";

    [JsonPropertyName("science")]
    public int Science { get; set; }

    [JsonPropertyName("player")]
    public int Player { get; set; } = -1;
}

public class SetPlayerVictoryPointsParameters : ToolCallParameters
{
    public override string ToolName => "setPlayerVictoryPoints";

    [JsonPropertyName("vp")]
    public int Vp { get; set; }

    [JsonPropertyName("player")]
    public int Player { get; set; } = -1;
}

public class ToolCallResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Error { get; set; }
} 