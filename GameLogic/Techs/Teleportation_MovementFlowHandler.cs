using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Teleportation_MovementFlowHandler : MovementFlowHandler<Tech_Teleportation>
{
    public Teleportation_MovementFlowHandler() : base("Teleportation")
    {
        AllowManyToOne = false;
        RequireAdjacency = false;
        ExhaustTechId = Tech_Teleportation.StaticId;
    }
}