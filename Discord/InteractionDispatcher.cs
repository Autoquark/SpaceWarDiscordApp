using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Google.Cloud.Firestore;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Discord.Commands;

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
    public static async Task HandleInteractionAsync<TBuilder>(TBuilder builder,
        InteractionData interactionData,
        Game game,
        IServiceProvider serviceProvider) where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        if (!game.DocumentId!.Equals(interactionData.Game))
        {
            throw new ArgumentException("InteractionData does not belong to the given game");
        }
        
        await HandleInteractionInternalAsync(builder, interactionData, game, serviceProvider);
    }

    /// <summary>
    /// Handles interactions sent from Discord
    /// </summary>
    public static async Task HandleInteractionCreated(DiscordClient client, InteractionCreatedEventArgs args)
    {
        if (args.Interaction.Type == DiscordInteractionType.ApplicationCommand || !Guid.TryParse(args.Interaction.Data.CustomId, out _))
        {
            return;
        }
        
        var builder = new DiscordWebhookBuilder().EnableV2Components();

        var snapshot = (await new Query<InteractionData>(Program.FirestoreDb.InteractionData()).WhereEqualTo(x => x.InteractionId, args.Interaction.Data.CustomId)
            .Limit(1)
            .GetSnapshotAsync()).FirstOrDefault();

        if (snapshot == null)
        {
            throw new Exception("InteractionData not found");
        }

        var interactionData = snapshot.ConvertToPolymorphic<InteractionData>();

        if (interactionData.EditOriginalMessage)
        {
            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
        }
        else
        {
            await args.Interaction.DeferAsync();
        }
        
        var game = await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.GetGameForChannelAsync(args.Interaction.ChannelId));

        if (game == null)
        {
            throw new Exception("Game not found");
        }
        
        var player = game.GetGamePlayerByDiscordId(args.Interaction.User.Id);
        if (player == null)
        {
            // Player is not part of this game, can't click any buttons
            return;
        }

        if (!interactionData.PlayerAllowedToTrigger(game, player))
        {
            await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent($"{args.Interaction.User.Mention} you can't click this, it not for you!"));
            return;
        }

        var serviceProvider = client.ServiceProvider.CreateScope().ServiceProvider;
        serviceProvider.GetRequiredService<SpaceWarCommandContextData>().GlobalData =
            await InteractionsHelper.GetGlobalDataAndIncrementInteractionGroupIdAsync();
        
        var outcome = await HandleInteractionInternalAsync(builder, interactionData, game, serviceProvider);

        if (outcome.RequiresSave)
        {
            try
            {
                await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));
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
            if (outcome.DeleteOriginalMessage)
            {
                if (outcome.ReplyBuilder is DiscordFollowupMessageBuilder followupBuilder)
                {
                    await args.Interaction.CreateFollowupMessageAsync(followupBuilder);
                }
                else
                {
                    await args.Interaction.CreateFollowupMessageAsync(
                        new DiscordFollowupMessageBuilder().EnableV2Components().AppendContentNewline($"ERROR: Tried to both delete and edit original message. Please report this to the developer ({interactionData.SubtypeName})"));
                }
            }
            else
            {
                if (outcome.ReplyBuilder is DiscordWebhookBuilder webhookBuilder)
                {
                    await args.Interaction.EditOriginalResponseAsync(webhookBuilder);
                }
                else if(outcome.ReplyBuilder is not null)
                {
                    await args.Interaction.CreateFollowupMessageAsync(
                        new DiscordFollowupMessageBuilder().EnableV2Components().AppendContentNewline($"ERROR: Invalid reply builder type. Please report this to the developer ({interactionData.SubtypeName})"));
                }
            }
        }
    }
    
    private static async Task<SpaceWarInteractionOutcome> HandleInteractionInternalAsync<TBuilder>(TBuilder builder,
        InteractionData interactionData,
        Game game,
        IServiceProvider serviceProvider) where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var interactionType = interactionData.GetType();
        if (!InteractionHandlers.TryGetValue(interactionType, out var handler))
        {
            throw new Exception("Handler not found");
        }

        return await (Task<SpaceWarInteractionOutcome>) typeof(IInteractionHandler<>).MakeGenericType(interactionType)
            .GetMethod(nameof(IInteractionHandler<InteractionData>.HandleInteractionAsync))!
            .MakeGenericMethod(typeof(DiscordWebhookBuilder))
            .Invoke(handler, [builder, interactionData, game, serviceProvider])!;
    }
}