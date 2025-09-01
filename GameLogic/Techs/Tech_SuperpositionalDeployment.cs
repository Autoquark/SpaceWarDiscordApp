using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_SuperpositionalDeployment : Tech
{
    public Tech_SuperpositionalDeployment() : base("superpositionalDeployment",
        "Superpositional Deployment",
        "Move any number of forces from one planet you control to an adjacent planet you control.",
        "It's not so much travelling as always having been there.",
        ["Free Action", "Once per turn"])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
        SimpleActionIsOncePerTurn = true;
        _movementFlowHandler = new SuperpositionalDeployment_MovementFlowHandler(this);
        AdditionalHandlers = [_movementFlowHandler];
    }

    private readonly SuperpositionalDeployment_MovementFlowHandler _movementFlowHandler;

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player) 
        => base.IsSimpleActionAvailable(game, player) && game.Hexes.WhereOwnedBy(player).Any(x => BoardUtils.GetNeighbouringHexes(game, x).WhereOwnedBy(player).Any());

    public override Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
        => _movementFlowHandler.BeginPlanningMoveAsync(builder, game, player, serviceProvider);
}

public class SuperpositionalDeployment_MovementFlowHandler : MovementFlowHandler<Tech_SuperpositionalDeployment>
{
    public SuperpositionalDeployment_MovementFlowHandler(Tech_SuperpositionalDeployment tech) : base(tech)
    {
        AllowManyToOne = false;
        MarkUsedTechId = tech.Id;
        ActionType = tech.SimpleActionType;
        DestinationRestriction = MoveDestinationRestriction.MustAlreadyControl;
    }
}