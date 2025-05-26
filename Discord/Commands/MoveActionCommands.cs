using SpaceWarDiscordApp.Discord.ContextChecks;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.Discord.Commands;

[RequireGameChannel]
public class MoveActionCommands : MovementFlowHandler<MoveActionCommands>
{
    public MoveActionCommands() : base("Move Action")
    {
    }
}