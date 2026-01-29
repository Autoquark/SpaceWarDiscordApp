using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents.Movement;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_FreeMoveVariant : Tech
{
    public const string StaticId = "free-move-variant";
    
    public Tech_FreeMoveVariant() : base(StaticId, "Free Move", "Make a move action. Exhaust the destination planet.", "", [TechKeyword.FreeAction, TechKeyword.OncePerTurn])
    {
        IncludeInGames = false;
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
        SimpleActionIsOncePerTurn = true;
        
        _movementFlowHandler = new FreeMoveVariant_MovementFlowHandler(this);
        AdditionalHandlers = [_movementFlowHandler];
    }
    
    private readonly FreeMoveVariant_MovementFlowHandler _movementFlowHandler;

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider) => await _movementFlowHandler.BeginPlanningMoveAsync(builder, game, player, serviceProvider);
}

class FreeMoveVariant_MovementFlowHandler : MovementFlowHandler<Tech_FreeMoveVariant>
{
    public FreeMoveVariant_MovementFlowHandler(Tech_FreeMoveVariant tech) : base(tech)
    {
        ActionType = GameLogic.ActionType.Free;
        ExhaustTechId = Tech_FreeMoveVariant.StaticId;
    }
    
    public override Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent_MovementFlowComplete<Tech_FreeMoveVariant> gameEvent, Game game,
        IServiceProvider serviceProvider)
    {
        game.GetHexAt(gameEvent.Destination).Planet!.IsExhausted = true;
        builder?.AppendContentNewline($"{gameEvent.Destination} has been exhausted");
        return base.HandleEventResolvedAsync(builder, gameEvent, game, serviceProvider);
    }
}