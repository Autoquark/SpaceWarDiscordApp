using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.MessageCommands;
using DSharpPlus.Entities;
using Grpc.Core;
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
        
        var outcome = context.ServiceProvider.GetRequiredService<SpaceWarCommandOutcome>();
        var contextData = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>();

        // The attributes on the command object include every attribute from the command method but only context check attributes
        // from the containing class, so we need to check manually for an attribute on an outer class
        var requiresGameAttribute = context.Command.Attributes.OfType<RequireGameChannelAttribute>().FirstOrDefault();
        var type = context.Command.Method!.DeclaringType;
        while(type != null && requiresGameAttribute == null)
        {
            requiresGameAttribute = type.GetCustomAttribute<RequireGameChannelAttribute>();
            type = type.BaseType;
        }
        
        var requiresGame = requiresGameAttribute != null && requiresGameAttribute.Mode != RequireGameChannelMode.DoNotRequire;
        if (requiresGame)
        {
            // Attempt to find the relevant game for this channel and store it in the context data
            contextData.Game = await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.GetGameForChannelAsync(context.Channel.Id), cancellationToken: cancellationToken);
            if (contextData.Game == null)
            {
                await context.EditResponseAsync("This command can only be used from a game channel");
                return;
            }
            
            // Set the default value in the outcome from any attribute that exists. Command code can override by setting
            // this value dynamically if necessary.
            outcome.RequiresSave = requiresGameAttribute!.Mode == RequireGameChannelMode.RequiresSave;
        }
        
        await base.ExecuteAsync(context, cancellationToken);

        // Commands that require a game must specify whether a game save is required, either via attribute or dynamically
        // via SpaceWarCommandOutcome
        if (requiresGame)
        {
            if (!outcome.RequiresSave.HasValue)
            {
                outcome.SetSimpleReply("ERROR: Command failed to set RequiresSave. Please report this to the developer.");
            }
            else if (outcome.RequiresSave == true)
            {
                try
                {
                    await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(contextData.Game!), cancellationToken: cancellationToken);
                }
                catch (RpcException)
                {
                    outcome.SetSimpleReply("ERROR: Failed to save game state. Please report this to the developer.");
                    throw;
                }
            }
        }

        if (outcome.ReplyBuilder != null)
        {
            await context.EditResponseAsync(outcome.ReplyBuilder);
        }
    }
}