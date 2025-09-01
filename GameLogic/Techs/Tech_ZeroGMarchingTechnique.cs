using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_ZeroGMarchingTechnique : Tech
{
    public Tech_ZeroGMarchingTechnique() : base(StaticId,
        "Zero G Marching Technique",
        "Perform a Move action.",
        "Left! Right! Left! Right! Hey, you! Stop floating!",
        ["Free Action", "Exhaust"])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
        _movementFlowHandler = new ZeroGMarchingTechnique_MovementFlowHandler(this);
        AdditionalHandlers = [_movementFlowHandler];
    }
    
    private readonly ZeroGMarchingTechnique_MovementFlowHandler _movementFlowHandler;

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider) => await _movementFlowHandler.BeginPlanningMoveAsync(builder, game, player, serviceProvider);

    public static string StaticId => "zeroGMarchingTechnique";
}

class ZeroGMarchingTechnique_MovementFlowHandler : MovementFlowHandler<Tech_ZeroGMarchingTechnique>
{
    public ZeroGMarchingTechnique_MovementFlowHandler(Tech_ZeroGMarchingTechnique tech) : base(tech)
    {
        ActionType = GameLogic.ActionType.Free;
        ExhaustTechId = Tech_ZeroGMarchingTechnique.StaticId;
    }
}