using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_ZeroGMarchingTechnique : Tech
{
    public Tech_ZeroGMarchingTechnique() : base(StaticId,
        "Zero G Marching Technique",
        "Free Action, Exhaust: Perform a Move action.",
        "Left! Right! Left! Right! Hey, you! Stop floating!")
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
    }


    public static string StaticId => "zeroGMarchingTechnique";
}

class ZeroGMarchingTechnique_MovementFlowHandler : MovementFlowHandler<Tech_ZeroGMarchingTechnique>
{
    public ZeroGMarchingTechnique_MovementFlowHandler() : base("Zero G Marching Technique")
    {
        ActionType = ActionType.Free;
        ExhaustTechId = Tech_ZeroGMarchingTechnique.StaticId;
    }
}