using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_HyperspaceRailway : Tech
{
    public Tech_HyperspaceRailway() : base("hyperspaceRailway",
        "Hyperspace Railway",
        "Move any number of forces from one planet you control to another planet you control.",
        "The 7.15 service to Alpha Centauri has been delayed due to leaves on the toroidal manifold.",
        [TechKeyword.FreeAction, TechKeyword.Exhaust])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
        _movementFlowHandler = new HyperspaceRailway_MovementFlowHandler(this);
        AdditionalHandlers = [_movementFlowHandler];
    }
    
    private readonly HyperspaceRailway_MovementFlowHandler _movementFlowHandler;

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
        => await _movementFlowHandler.BeginPlanningMoveAsync(builder, game, player, serviceProvider);

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player) => 
        base.IsSimpleActionAvailable(game, player) && game.Hexes.WhereOwnedBy(player).Count() > 1;
}

public class HyperspaceRailway_MovementFlowHandler : MovementFlowHandler<Tech_HyperspaceRailway>
{
    public HyperspaceRailway_MovementFlowHandler(Tech_HyperspaceRailway tech) : base(tech)
    {
        AllowManyToOne = false;
        RequireAdjacency = false;
        DestinationRestriction = MoveDestinationRestriction.MustAlreadyControl;
        ExhaustTechId = tech.Id;
        ActionType = tech.SimpleActionType;
    }
}