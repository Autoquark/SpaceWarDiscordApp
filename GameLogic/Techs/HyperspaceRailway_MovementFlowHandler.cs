using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class HyperspaceRailway_MovementFlowHandler : MovementFlowHandler<Tech_HyperspaceRailway>
{
    public HyperspaceRailway_MovementFlowHandler() : base("Hyperspace Railway")
    {
        AllowManyToOne = false;
        RequireAdjacency = false;
        DestinationRestriction = MoveDestinationRestriction.MustAlreadyControl;
    }
}