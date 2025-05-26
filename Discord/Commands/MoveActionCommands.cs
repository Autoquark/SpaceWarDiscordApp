using SpaceWarDiscordApp.Discord.ContextChecks;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.Discord.Commands;

[RequireGameChannel]
public class MoveActionCommands : PlanMovementFlowHandler<MoveActionCommands>
{
    public MoveActionCommands() : base("Move Action")
    {
    }
}