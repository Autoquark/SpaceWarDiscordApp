using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_Teleportation : Tech
{
    public const string StaticId = "teleportation";
    
    public Tech_Teleportation() : base(StaticId,
        "Teleportation",
        "Action: Move any number of forces from one planet you control to any other planet.",
        "Teleportation isn't an exact science. As long as total limbs in = total limbs out we consider it a success")
    {
        HasSimpleAction = true;
        AdditionalInteractionHandlers = [_movementFlowHandler];
    }

    private readonly Teleportation_MovementFlowHandler _movementFlowHandler = new(); 

    public override async Task<TBuilder> UseTechActionAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player)
        => await _movementFlowHandler.BeginPlanningMoveAsync(builder, game, player);
}

public class Teleportation_MovementFlowHandler : MovementFlowHandler<Tech_Teleportation>
{
    public Teleportation_MovementFlowHandler() : base("Teleportation")
    {
        AllowManyToOne = false;
        RequireAdjacency = false;
        ExhaustTechId = Tech_Teleportation.StaticId;
    }
}