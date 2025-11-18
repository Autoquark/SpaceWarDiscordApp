using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.MessageCommands;
using DSharpPlus.Entities;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Discord.ContextChecks;

namespace SpaceWarDiscordApp.Discord;

// Definitely the most dramatic class name I've ever come up with
public class SpaceWarCommandExecutor : DefaultCommandExecutor
{
    
    public override async ValueTask ExecuteAsync(CommandContext context, CancellationToken cancellationToken = new())
    {
        if (!Program.BotReady)
        {
            await context.RespondAsync(new DiscordMessageBuilder().WithContent("Bot is starting up, please try again in a few seconds."));
        }
        
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
        
        var cache = context.Client.ServiceProvider.GetRequiredService<GameCache>();

        SemaphoreSlim? semaphore = null;
        try
        {
            var requiresGame = requiresGameAttribute != null &&
                               requiresGameAttribute.Mode != RequireGameChannelMode.DoNotRequire;
            if (requiresGame)
            {
                // Attempt to find the relevant game for this channel and store it in the context data
                contextData.Game = cache.GetGame(context.Channel.Id)
                                   ?? await Program.FirestoreDb.RunTransactionAsync(
                                       transaction => transaction.GetGameForChannelAsync(context.Channel.Id),
                                       cancellationToken: cancellationToken);

                if (contextData.Game == null)
                {
                    await context.EditResponseAsync("This command can only be used from a game channel");
                    return;
                }

                var syncManager = context.Client.ServiceProvider.GetRequiredService<GameSyncManager>();
                semaphore = syncManager.GetSemaphoreForGame(contextData.Game);
                if (!semaphore.Wait(0, cancellationToken))
                {
                    // Prevents trying to release the semaphore, since we did not acquire it
                    
                    semaphore = null;
                    await context.EditResponseAsync(
                        "An operation is already in progress for this game. Please wait for it to finish before trying again.");
                    return;
                }

                cache.AddOrUpdateGame(contextData.Game!);

                if (requiresGameAttribute!.RequiredPhase != null &&
                    contextData.Game.Phase != requiresGameAttribute.RequiredPhase)
                {
                    await context.EditResponseAsync(
                        $"This command can only be used in the {requiresGameAttribute.RequiredPhase} phase of the game.");
                    return;
                }

                // Set the default value in the outcome from any attribute that exists. Command code can override by setting
                // this value dynamically if necessary.
                outcome.RequiresSave = requiresGameAttribute!.Mode == RequireGameChannelMode.RequiresSave;
            }

            contextData.GlobalData = await InteractionsHelper.GetGlobalDataAndIncrementInteractionGroupIdAsync();
            contextData.User = context.User;
            
            var interactionsToSetUp = context.ServiceProvider.GetInteractionsToSetUp();

            await base.ExecuteAsync(context, cancellationToken);

            // Commands that require a game must specify whether a game save is required, either via attribute or dynamically
            // via SpaceWarCommandOutcome
            if (requiresGame)
            {
                if (!outcome.RequiresSave.HasValue)
                {
                    outcome.SetSimpleReply(
                        "ERROR: Command failed to set RequiresSave. Please report this to the developer.");
                }
                else if (outcome.RequiresSave == true || interactionsToSetUp.Any())
                {
                    try
                    {
                        await Program.FirestoreDb.RunTransactionAsync(transaction =>
                            {
                                if (outcome.RequiresSave == true)
                                {
                                    transaction.Set(contextData.Game!);
                                }
                                
                                InteractionsHelper.SetUpInteractions(interactionsToSetUp,
                                    transaction,
                                    contextData.GlobalData.InteractionGroupId);
                            },
                            cancellationToken: cancellationToken);
                    }
                    catch (RpcException)
                    {
                        outcome.SetSimpleReply(
                            "ERROR: Failed to save game state. Please report this to the developer.");
                        throw;
                    }
                }
            }
            else if (interactionsToSetUp.Count != 0)
            {
                await Program.FirestoreDb.RunTransactionAsync(transaction =>
                    InteractionsHelper.SetUpInteractions(interactionsToSetUp, transaction,
                        contextData.GlobalData.InteractionGroupId), cancellationToken: cancellationToken);
            }

            if (outcome.ReplyBuilder != null)
            {
                var first = outcome.ReplyBuilder.Builders.First();
                await context.EditResponseAsync(first);

                foreach (var followupBuilder in outcome.ReplyBuilder.Builders.Skip(1))
                {
                    await context.FollowupAsync(followupBuilder);
                }
            }
        }
        catch (Exception e) when (!Program.IsTestEnvironment)
        {
            // Force a refetch next command so any half-complete operations on the in-memory game object are discarded
            if (contextData.Game?.DocumentId != null)
            {
                cache.Clear(contextData.Game.DocumentId);
            }

            await Program.LogExceptionAsync(contextData.Game, e);
            
            await context.EditResponseAsync("An error occurred. Please try again, or report as a bug if the problem persists");
            throw;
        }
        finally
        {
            // If we acquired a game access semaphore ensure we release it 
            semaphore?.Release();
        }
    }
}