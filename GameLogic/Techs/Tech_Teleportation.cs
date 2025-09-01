using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_Teleportation : Tech
{
    public const string StaticId = "teleportation";
    
    public Tech_Teleportation() : base(StaticId,
        "Teleportation",
        "Move any number of forces from one planet you control to any other planet.",
        "Teleportation isn't an exact science. As long as total limbs in = total limbs out we consider it a success",
        ["Action", "Exhaust"])
    {
        HasSimpleAction = true;
        _movementFlowHandler = new Teleportation_MovementFlowHandler(this);
        AdditionalHandlers = [_movementFlowHandler];
    }

    private readonly Teleportation_MovementFlowHandler _movementFlowHandler; 

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
        => await _movementFlowHandler.BeginPlanningMoveAsync(builder, game, player, serviceProvider);
}

public class Teleportation_MovementFlowHandler : MovementFlowHandler<Tech_Teleportation>
{
    public Teleportation_MovementFlowHandler(Tech_Teleportation tech) : base(tech)
    {
        AllowManyToOne = false;
        RequireAdjacency = false;
        ExhaustTechId = Tech_Teleportation.StaticId;
    }
}