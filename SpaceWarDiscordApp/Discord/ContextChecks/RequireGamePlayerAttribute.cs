using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;

namespace SpaceWarDiscordApp.Discord.ContextChecks;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireGamePlayerAttribute : Attribute
{
    
}