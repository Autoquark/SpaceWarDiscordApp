using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using Microsoft.Extensions.DependencyInjection;

namespace SpaceWarDiscordApp.Discord.ContextChecks;

public class RequireGameChannelCheck : IContextCheck<RequireGameChannelAttribute>
{
    public ValueTask<string?> ExecuteCheckAsync(RequireGameChannelAttribute attribute, CommandContext context)
    {
        return context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game == null ?
            new ValueTask<string?>("This command can only be used from a game channel")
            : new ValueTask<string?>();
    }
}