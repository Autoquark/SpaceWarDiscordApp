using SpaceWarDiscordApp.Discord.ContextChecks;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.Discord.Commands;

public class MoveActionCommands : MovementFlowHandler<MoveActionCommands>
{
    public MoveActionCommands() : base(null)
    {
    }
}