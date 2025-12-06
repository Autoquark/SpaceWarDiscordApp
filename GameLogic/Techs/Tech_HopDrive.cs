using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_HopDrive : Tech
{
    public Tech_HopDrive() : base("hopDrive", "Hop Drive",
        "Move any number of your forces from one planet to another that is exactly 2 hexes away.",
        "Trust me, with this thing it's much better to arrive than to travel. In fact, if you want my advice, stay at home.",
        [TechKeyword.Action])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Main;
        _movementFlowHandler = new HopDriveMovementFlowHandler(this);
        AdditionalHandlers = [_movementFlowHandler];
    }

    private readonly HopDriveMovementFlowHandler _movementFlowHandler;
    
    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        await _movementFlowHandler.BeginPlanningMoveAsync(builder, game, player, serviceProvider);
        
        return builder;
    }
}

class HopDriveMovementFlowHandler : MovementFlowHandler<Tech_HopDrive>
{
    public HopDriveMovementFlowHandler(Tech_HopDrive tech) : base(tech)
    {
        AllowManyToOne = false;
        ActionType = tech.SimpleActionType;
    }

    protected override ISet<BoardHex> GetAllowedDestinationsForSource(Game game, BoardHex source)
    {
        var oneAway = BoardUtils.GetNeighbouringHexes(game, source.Coordinates);
        return oneAway
            .SelectMany(x => BoardUtils.GetNeighbouringHexes(game, x.Coordinates))
            .Except(oneAway)
            .Except(source)
            .ToHashSet();
    }

    protected override List<BoardHex> GetAllowedMoveSources(Game game, GamePlayer player, BoardHex? destination)
    {
        if (destination == null)
        {
            return base.GetAllowedMoveSources(game, player, destination);
        }
        
        var oneAway = BoardUtils.GetNeighbouringHexes(game, destination.Coordinates);
        return oneAway
            .SelectMany(x => BoardUtils.GetNeighbouringHexes(game, x.Coordinates))
            .WhereOwnedBy(player)
            .Except(oneAway)
            .Except(destination)
            .ToList();
    }
}