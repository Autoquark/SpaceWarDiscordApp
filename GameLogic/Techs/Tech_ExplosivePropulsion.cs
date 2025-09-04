using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_ExplosivePropulsion : Tech
{
    public Tech_ExplosivePropulsion() : base("explosive-propulsion",
        "Explosive Propulsion",
        "Choose a ready planet you control. Exhaust it and move any number of forces from it to an adjacent planet.",
        "They told me that I should wait until we were clear of the atmosphere before I hit this button, but I don't think their opinions are going to be relevant for much longer.",
        ["Free Action", "Once per turn"])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
        SimpleActionIsOncePerTurn = true;
        _movementFlowHandler = new ExplosivePropulsionMovementFlowHandler(this);
        AdditionalHandlers = [_movementFlowHandler];
    }
    
    private readonly ExplosivePropulsionMovementFlowHandler _movementFlowHandler;

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
        => await _movementFlowHandler.BeginPlanningMoveAsync(builder, game, player, serviceProvider);
}

class ExplosivePropulsionMovementFlowHandler : MovementFlowHandler<Tech_ExplosivePropulsion>
{
    public ExplosivePropulsionMovementFlowHandler(Tech_ExplosivePropulsion tech) : base(tech)
    {
        ActionType = tech.SimpleActionType;
        AllowManyToOne = false;
        MarkUsedTechId = tech.Id;
    }

    protected override List<BoardHex> GetAllowedMoveSources(Game game, GamePlayer player, BoardHex destination)
        => base.GetAllowedMoveSources(game, player, destination).Where(x => !x.Planet!.IsExhausted).ToList();

    public override Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder,
        GameEvent_MovementFlowComplete<Tech_ExplosivePropulsion> gameEvent,
        Game game,
        IServiceProvider serviceProvider)
    {
        builder?.AppendContentNewline($"{gameEvent.Sources.Single().Source} has been exhausted");
        game.GetHexAt(gameEvent.Sources.Single().Source).Planet!.IsExhausted = true;
        return base.HandleEventResolvedAsync(builder, gameEvent, game, serviceProvider);
    }
}