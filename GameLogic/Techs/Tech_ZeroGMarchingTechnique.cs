using SpaceWarDiscordApp.Database;
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
        AdditionalInteractionHandlers = [_movementFlowHandler];
    }
    
    private readonly ZeroGMarchingTechnique_MovementFlowHandler _movementFlowHandler = new();

    public override async Task<TBuilder> UseTechActionAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider) => await _movementFlowHandler.BeginPlanningMoveAsync(builder, game, player, serviceProvider);

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