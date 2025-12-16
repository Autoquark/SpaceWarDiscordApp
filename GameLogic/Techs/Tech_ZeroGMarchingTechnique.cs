using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Movement;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_ZeroGMarchingTechnique : Tech
{
    public Tech_ZeroGMarchingTechnique() : base(StaticId,
        "Zero G Marching Technique",
        "Perform a Move action. Exhaust the destination planet.",
        "Left! Right! Left! Right! Hey, you! Stop floating!",
        [TechKeyword.FreeAction, TechKeyword.Exhaust])
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
    
    public override Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent_MovementFlowComplete<Tech_ZeroGMarchingTechnique> gameEvent, Game game,
        IServiceProvider serviceProvider)
    {
        game.GetHexAt(gameEvent.Destination).Planet!.IsExhausted = true;
        builder?.AppendContentNewline($"{gameEvent.Destination} has been exhausted");
        return base.HandleEventResolvedAsync(builder, gameEvent, game, serviceProvider);
    }
}