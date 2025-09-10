using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_SolarTricycle : Tech
{
    public const string StaticId = "solarTricycle";
    
    public Tech_SolarTricycle() : base(StaticId,
        "Solar Tricycle",
        "Move 1 of your forces to an adjacent planet. Can only be used after you have taken your main action.",
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

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player) => base.IsSimpleActionAvailable(game, player) && game.ActionTakenThisTurn;

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

    public override Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent_MovementFlowComplete<Tech_SolarTricycle> gameEvent, Game game,
        IServiceProvider serviceProvider)
    {
        game.GetHexAt(gameEvent.Destination).Planet!.IsExhausted = true;
        builder?.AppendContentNewline($"{gameEvent.Destination} has been exhausted");
        return base.HandleEventResolvedAsync(builder, gameEvent, game, serviceProvider);
    }
}