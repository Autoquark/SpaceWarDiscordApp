using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_SolarTricycle : Tech
{
    public const string StaticId = "solarTricycle";
    
    public Tech_SolarTricycle() : base(StaticId,
        "Solar Tricycle",
        "Move 1 of your forces to an adjacent planet.",
        "It's both ecologically friendly and a great form of exercise!",
        ["Free Action", "Once per turn"])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
        SimpleActionIsOncePerTurn = true;
        _movementFlowHandler = new SolarTricycle_MovementFlowHandler(this);
        AdditionalHandlers = [_movementFlowHandler];
    }
    
    private readonly SolarTricycle_MovementFlowHandler _movementFlowHandler;

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
        => await _movementFlowHandler.BeginPlanningMoveAsync(builder, game, player, serviceProvider);
}

public class SolarTricycle_MovementFlowHandler : MovementFlowHandler<Tech_SolarTricycle>
{
    public SolarTricycle_MovementFlowHandler(Tech_SolarTricycle tech) : base(tech)
    {
        AllowManyToOne = false;
        MarkUsedTechId = tech.Id;
        StaticMaxAmountPerSource = 1;
        ActionType = tech.SimpleActionType;
    }
}