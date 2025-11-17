using System.Reflection;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Google.Cloud.Firestore;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.Discord;

public static class InteractionDispatcher
{
    private static readonly Dictionary<Type, object> InteractionHandlers = new();
    
    public static void RegisterInteractionHandler(object interactionHandler)
    {
        foreach (var interactionType in interactionHandler.GetType()
                     .GetInterfaces()
                     .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IInteractionHandler<>))
                     .Select(x => x.GetGenericArguments()[0]))
        {
            if (!InteractionHandlers.TryAdd(interactionType, interactionHandler))
            {
                throw new Exception($"Handler already registered for {interactionType}");
            }
        }
        
    }
    
    /// <summary>
    /// Allows game logic to trigger resolution of an interaction directly
    /// </summary>
    public static async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        InteractionData gamePlayerInteractionData,
        Game game,
        IServiceProvider serviceProvider)
    {
        if (!game.DocumentId!.Equals(gamePlayerInteractionData.Game))
        {
            throw new ArgumentException("InteractionData does not belong to the given game");
        }
        
        return await HandleInteractionInternalAsync(builder, gamePlayerInteractionData, game, serviceProvider);
    }

    /// <summary>
    /// Handles interactions sent from Discord
    /// </summary>
    public static async Task HandleInteractionCreated(DiscordClient client, InteractionCreatedEventArgs args)
    {
        if (!Program.BotReady)
        {
            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Bot is starting up, please try again in a few seconds.").AsEphemeral());
        }
        
        if (args.Interaction.Type == DiscordInteractionType.ApplicationCommand || !Guid.TryParse(args.Interaction.Data.CustomId, out _))
        {
            return;
        }
        
        var builder = new DiscordMultiMessageBuilder(new DiscordWebhookBuilder(), () => new DiscordFollowupMessageBuilder());

        if (!Guid.TryParse(args.Interaction.Data.CustomId, out var interactionId))
        {
            return;
        }

        var interactionData =
            await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.GetInteractionDataAsync(interactionId));

        if (interactionData == null)
        {
            throw new Exception("InteractionData not found");
        }

        if (interactionData.EditOriginalMessage)
        {
            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
        }
        else
        {
            await args.Interaction.DeferAsync(interactionData.EphemeralResponse);
        }

        var cache = client.ServiceProvider.GetRequiredService<GameCache>();
        var game = cache.GetGame(interactionData.Game!);
        if (game == null)
        {
            game = await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.GetGameAsync(interactionData.Game!));
        }

        if (game == null)
        {
            throw new Exception("Game not found");
        }
        
        if (!interactionData.UserAllowedToTrigger(game, args.Interaction.User))
        {
            await args.Interaction.CreateFollowupMessageAsync(
                new DiscordFollowupMessageBuilder()
                    .WithContent($"{args.Interaction.User.Mention} you can't click this, it not for you!")
                    .AsEphemeral());
            return;
        }
        
        var syncManager = client.ServiceProvider.GetRequiredService<GameSyncManager>();
        SemaphoreSlim? semaphore = null;
        try
        {
            semaphore = syncManager.GetSemaphoreForGame(game);
            if (!semaphore.Wait(0))
            {
                // Prevents trying to release the semaphore, since we did not acquire it
                semaphore = null;
                await args.Interaction.CreateFollowupMessageAsync(
                    new DiscordFollowupMessageBuilder()
                        .WithContent(
                            "An operation is already in progress for this game. Please wait for it to finish before trying again.")
                        .AsEphemeral());
                return;
            }

            cache.AddOrUpdateGame(game);

            var serviceProvider = client.ServiceProvider.CreateScope().ServiceProvider;
            var contextData = serviceProvider.GetRequiredService<SpaceWarCommandContextData>();
            contextData.GlobalData = await InteractionsHelper.GetGlobalDataAndIncrementInteractionGroupIdAsync();
            contextData.Game = game;
            contextData.User = args.Interaction.User;
            contextData.InteractionMessage = args.Interaction.Message;

            var interactionsToSetUp = serviceProvider.GetInteractionsToSetUp();

            var outcome = await HandleInteractionInternalAsync(builder, interactionData, game, serviceProvider);

            if (outcome.RequiresSave || interactionsToSetUp.Any())
            {
                try
                {
                    await Program.FirestoreDb.RunTransactionAsync(transaction =>
                    {
                        if (outcome.RequiresSave)
                        {
                            transaction.Set(game);
                        }

                        InteractionsHelper.SetUpInteractions(interactionsToSetUp,
                            transaction,
                            contextData.GlobalData.InteractionGroupId);
                    });
                }
                catch (RpcException)
                {
                    outcome.SetSimpleReply("ERROR: Failed to save game state. Please report this to the developer.");
                    throw;
                }
            }

            if (outcome.DeleteOriginalMessage)
            {
                await args.Interaction.DeleteOriginalResponseAsync();
            }

            if (outcome.ReplyBuilder != null)
            {
                var firstBuilder = outcome.ReplyBuilder.Builders.First();
                if (firstBuilder.Components.Count > 0)
                {
                    if (outcome.DeleteOriginalMessage)
                    {
                        if (firstBuilder is DiscordFollowupMessageBuilder followupBuilder)
                        {
                            await args.Interaction.CreateFollowupMessageAsync(followupBuilder);
                        }
                        else
                        {
                            await args.Interaction.CreateFollowupMessageAsync(
                                new DiscordFollowupMessageBuilder()
                                    .EnableV2Components()
                                    .AppendContentNewline(
                                        $"ERROR: Tried to both delete and edit original message. Please report this to the developer ({interactionData.SubtypeName})"));
                        }
                    }
                    else
                    {
                        if (firstBuilder is DiscordWebhookBuilder webhookBuilder)
                        {
                            await args.Interaction.EditOriginalResponseAsync(webhookBuilder);
                        }
                        else if (outcome.ReplyBuilder is not null)
                        {
                            await args.Interaction.CreateFollowupMessageAsync(
                                new DiscordFollowupMessageBuilder().EnableV2Components().AppendContentNewline(
                                    $"ERROR: Invalid reply builder type. Please report this to the developer ({interactionData.SubtypeName})"));
                        }
                    }

                    foreach (var followupBuilder in outcome.ReplyBuilder!.Builders.Skip(1)
                                 .Cast<DiscordFollowupMessageBuilder>())
                    {
                        await args.Interaction.CreateFollowupMessageAsync(followupBuilder);
                    }
                }
            }
        }
        catch (Exception e)
        {
            // Force a refetch next command so any half complete operations on the in-memory game object are discarded
            if (game.DocumentId != null)
            {
                cache.Clear(game.DocumentId);
            }

            await Program.LogExceptionAsync(game, e);

            await args.Interaction.EditOriginalResponseAsync(
                new DiscordWebhookBuilder().WithContent(
                    "An error occurred. Please try again, or report as a bug if the problem persists"));
            throw;
        }
        finally
        {
            semaphore?.Release();
        }
    }
    
    private static async Task<SpaceWarInteractionOutcome> HandleInteractionInternalAsync(DiscordMultiMessageBuilder? builder,
        InteractionData interaction,
        Game game,
        IServiceProvider serviceProvider) =>
        await (Task<SpaceWarInteractionOutcome>) typeof(InteractionDispatcher).GetMethod(nameof(HandleTypedInteractionInternalAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(interaction.GetType())
            .Invoke(null, [builder, interaction, game, serviceProvider])!;

    private static async Task<SpaceWarInteractionOutcome> HandleTypedInteractionInternalAsync<TInteractionData>(DiscordMultiMessageBuilder? builder,
        TInteractionData interaction,
        Game game,
        IServiceProvider serviceProvider)
        where TInteractionData : InteractionData
    {
        var currentEvent = game.EventStack.LastOrDefault();
        if (interaction.ResolvesChoiceEvent != null)
        {
            if (currentEvent is GameEvent_PlayerChoice<TInteractionData> choiceEvent)
            {
                await (Task<DiscordMultiMessageBuilder?>) typeof(GameEventDispatcher).GetMethod(nameof(GameEventDispatcher.HandlePlayerChoiceEventResolvedAsync))!
                    .MakeGenericMethod(currentEvent.GetType(), typeof(TInteractionData))
                    .Invoke(null, [builder, choiceEvent, interaction, game, serviceProvider])!;
                return new SpaceWarInteractionOutcome(true, builder);
            }
            else
            {
                builder?.AppendContentNewline("These buttons are not for the currently resolving effect.");
                return new SpaceWarInteractionOutcome(false, builder);
            }
        }
        
        var interactionType = interaction.GetType();
        if (!InteractionHandlers.TryGetValue(interactionType, out var handler))
        {
            throw new Exception("Handler not found");
        }

        if (interaction is EventModifyingInteractionData eventModifyingInteractionData)
        {
            // If this is an EventModifyingInteractionData, populate the event property
            var baseType = interactionType;
            while (baseType != null &&
                   (!baseType.IsGenericType ||
                    baseType.GetGenericTypeDefinition() != typeof(EventModifyingInteractionData<>)))
            {
                baseType = baseType.BaseType;
            }

            if (baseType != null)
            {
                var eventType = baseType.GetGenericArguments()[0];
                // Document id check for the remote scenario where the top event on the stack is of the right type, but not actually the event that this trigger is associated with
                if (currentEvent == null || currentEvent.GetType() != eventType || !eventModifyingInteractionData.EventDocumentId.Equals(currentEvent.DocumentId))
                {
                    builder?.AppendContentNewline("These buttons are not for the currently resolving effect.");
                    return new SpaceWarInteractionOutcome(false, builder);
                }

                interactionType.GetProperty(nameof(EventModifyingInteractionData<GameEvent>.Event))!
                    .SetValue(interaction, currentEvent);
            }
        }

        return await ((IInteractionHandler<TInteractionData>) handler).HandleInteractionAsync(builder, interaction, game, serviceProvider);
    }
}