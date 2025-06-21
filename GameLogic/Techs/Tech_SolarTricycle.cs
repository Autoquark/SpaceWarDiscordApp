using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_SolarTricycle : Tech
{
    public const string StaticId = "solarTricycle";
    
    public Tech_SolarTricycle() : base(StaticId,
        "Solar Tricycle",
        "Free Action, Once per turn: Move 1 of your forces to an adjacent planet.",
        "It's both ecologically friendly and a great form of exercise!")
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
        SimpleActionIsOncePerTurn = true;
        _movementFlowHandler = new SolarTricycle_MovementFlowHandler(this);
        AdditionalInteractionHandlers = [_movementFlowHandler];
    }
    
    private readonly SolarTricycle_MovementFlowHandler _movementFlowHandler;

    public override async Task<TBuilder> UseTechActionAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player)
        => await _movementFlowHandler.BeginPlanningMoveAsync(builder, game, player);
}

public class SolarTricycle_MovementFlowHandler : MovementFlowHandler<Tech_SolarTricycle>
{
    public SolarTricycle_MovementFlowHandler(Tech_SolarTricycle tech) : base(tech.DisplayName)
    {
        AllowManyToOne = false;
        MarkUsedTechId = tech.Id;
        MaxAmountPerSource = 1;
        ActionType = tech.SimpleActionType;
    }
}