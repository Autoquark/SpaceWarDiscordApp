using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_MassMigration : Tech
{
    public Tech_MassMigration() : base("mass-migration2", "Mass Migration",
        "Move all of your forces from one planet to an adjacent planet.",
        "Due to recent budget cutbacks, the planetary government will unfortunately no longer be able to provide certain public services to residents, such as a breathable atmosphere.",
        ["Free Action", "Exhaust"])
    {
        HasSimpleAction = true;
        _movementFlowHandler = new MassMigrationMovementFlowHandler(this);
        AdditionalHandlers = [_movementFlowHandler];
    }
    
    private readonly MassMigrationMovementFlowHandler _movementFlowHandler;

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider) =>
        await _movementFlowHandler.BeginPlanningMoveAsync(builder, game, player, serviceProvider);
}

class MassMigrationMovementFlowHandler : MovementFlowHandler<Tech_MassMigration>
{
    public MassMigrationMovementFlowHandler(Tech_MassMigration tech) : base(tech)
    {
        AllowManyToOne = false;
        ActionType = tech.SimpleActionType;
        ContinueResolvingStackAfterMove = true;
        ExhaustTechId = tech.Id;
        MustMoveAll = true;
    }
}