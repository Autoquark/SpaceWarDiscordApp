using System.Diagnostics.CodeAnalysis;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.MessageCommands;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord.ContextChecks;

namespace SpaceWarDiscordApp.Discord;

// Definitely the most dramatic class name I've ever come up with
public class SpaceWarCommandExecutor : DefaultCommandExecutor
{
    public override async ValueTask ExecuteAsync(CommandContext context, CancellationToken cancellationToken = new())
    {
        await context.DeferResponseAsync();
        
        if (context.Command.Attributes.OfType<RequireGameChannelAttribute>().Any())
        {
            // Attempt to find the relevant game for this channel and store it in the context data
            var snapshot = await new Query<Game>(Program.FirestoreDb.Collection("Games"))
                .WhereEqualTo(x => x.GameChannelId, context.Channel.Id)
                .Limit(1)
                .GetSnapshotAsync();

            if (snapshot.Count > 0)
            {
                context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game =
                    snapshot[0].ConvertTo<Game>();
            }
        }

        await base.ExecuteAsync(context, cancellationToken);
    }
}