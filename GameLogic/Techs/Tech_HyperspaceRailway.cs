using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_HyperspaceRailway : Tech
{
    public Tech_HyperspaceRailway() : base("hyperspaceRailway",
        "Hyperspace Railway",
        "Action: Move any number of forces from one planet you control to another planet you control.",
        "The 7.15 service to Alpha Centauri has been delayed due to leaves on the toroidal manifold.")
    {
        HasSimpleAction = true;
        AdditionalInteractionHandlers = [_movementFlowHandler];
    }
    
    private readonly HyperspaceRailway_MovementFlowHandler _movementFlowHandler = new();

    public override async Task<TBuilder> UseTechActionAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player)
        => await _movementFlowHandler.BeginPlanningMoveAsync(builder, game, player);

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player) => 
        base.IsSimpleActionAvailable(game, player) && game.Hexes.WhereOwnedBy(player).Count() > 1;
}