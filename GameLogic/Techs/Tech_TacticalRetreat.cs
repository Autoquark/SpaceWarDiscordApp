using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_TacticalRetreat : Tech
{
    public Tech_TacticalRetreat() : base("tactical-retreat",
        "Tactical Retreat",
        "Move all of your forces from a planet to an adjacent planet that you control.",
        "It's a bold new brand concept for cowardice.",
        [ "Free Action" ])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
        _movementFlowHandler = new TacticalRetreatMovementFlowHandler(this);
        AdditionalHandlers = [_movementFlowHandler];
    }
    
    private readonly TacticalRetreatMovementFlowHandler _movementFlowHandler;

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider) =>
        await _movementFlowHandler.BeginPlanningMoveAsync(builder, game, player, serviceProvider);
}

class TacticalRetreatMovementFlowHandler : MovementFlowHandler<Tech_TacticalRetreat>
{
    public TacticalRetreatMovementFlowHandler(Tech_TacticalRetreat tech) : base(tech)
    {
        ActionType = tech.SimpleActionType;
        AllowManyToOne = false;
        DestinationRestriction = MoveDestinationRestriction.MustAlreadyControl;
        MustMoveAll = true;
    }
}