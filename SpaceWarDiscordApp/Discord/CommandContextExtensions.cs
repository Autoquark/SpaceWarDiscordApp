using DSharpPlus.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace SpaceWarDiscordApp.Discord;

public static class CommandContextExtensions
{
    public static SpaceWarCommandOutcome Outcome(this CommandContext context) =>
        context.ServiceProvider.GetRequiredService<SpaceWarCommandOutcome>();
}