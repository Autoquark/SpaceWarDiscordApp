using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_MagneticTransferPods : Tech
{
    public Tech_MagneticTransferPods() : base("magnetic-transfer-pods",
        "Magnetic Transfer Pods",
        "Move any number of forces from one planet you control to an adjacent planet you control.",
        "You'll be spending the next week in this small metal pod stuck to the hull of a tomato soup tanker. Any questions?",
        ["Free Action", "Once per turn"])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
        SimpleActionIsOncePerTurn = true;
        _movementFlowHandler = new MagneticTransferPods_MovementFlowHandler(this);
        AdditionalHandlers = [_movementFlowHandler];
    }

    private readonly MagneticTransferPods_MovementFlowHandler _movementFlowHandler;

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player) 
        => base.IsSimpleActionAvailable(game, player) && game.Hexes.WhereOwnedBy(player).Any(x => BoardUtils.GetNeighbouringHexes(game, x).WhereOwnedBy(player).Any());

    public override Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
        => _movementFlowHandler.BeginPlanningMoveAsync(builder, game, player, serviceProvider);
}

public class MagneticTransferPods_MovementFlowHandler : MovementFlowHandler<Tech_MagneticTransferPods>
{
    public MagneticTransferPods_MovementFlowHandler(Tech_MagneticTransferPods tech) : base(tech)
    {
        AllowManyToOne = false;
        MarkUsedTechId = tech.Id;
        ActionType = tech.SimpleActionType;
        DestinationRestriction = MoveDestinationRestriction.MustAlreadyControl;
    }
}