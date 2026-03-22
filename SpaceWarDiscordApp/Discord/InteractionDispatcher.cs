using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.Discord;

public static class InteractionDispatcher
{
    internal static readonly InteractionDispatcher<Game> Instance =
        new(GameEventDispatcher.Instance);

    public static void RegisterInteractionHandler(object interactionHandler)
        => Instance.RegisterInteractionHandler(interactionHandler);

    /// <summary>
    /// Allows game logic to trigger resolution of an interaction directly
    /// </summary>
    public static Task<InteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        InteractionData gamePlayerInteractionData,
        Game game,
        IServiceProvider serviceProvider)
        => Instance.HandleInteractionAsync(builder, gamePlayerInteractionData, game, serviceProvider);

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

        var cache = client.ServiceProvider.GetRequiredService<GameCache<Game, NonDbGameState>>();
        if (!cache.GetGame(interactionData.Game!, out var game, out var nonDbGameState))
        {
            game = await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.GetGameAsync(interactionData.Game!));
        }

        if (game == null)
        {
            throw new Exception("Game not found");
        }

        if (nonDbGameState == null)
        {
            nonDbGameState = new NonDbGameState();
            ProdOperations.UpdateProdTimers(game, nonDbGameState);
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
        using var semaphore = await syncManager.Locker.LockOrNullAsync(game.DocumentId!, 0);
        try
        {
            if (semaphore == null)
            {
                await args.Interaction.CreateFollowupMessageAsync(
                    new DiscordFollowupMessageBuilder()
                        .WithContent(
                            "An operation is already in progress for this game. Please wait for it to finish before trying again.")
                        .AsEphemeral());
                return;
            }

            cache.AddOrUpdateGame(game, nonDbGameState);

            var serviceProvider = client.ServiceProvider.CreateScope().ServiceProvider;
            var contextData = serviceProvider.GetRequiredService<SpaceWarCommandContextData>();
            contextData.GlobalData = await InteractionsHelper.GetGlobalDataAndIncrementInteractionGroupIdAsync();
            contextData.Game = game;
            contextData.User = args.Interaction.User;
            contextData.InteractionMessage = args.Interaction.Message;
            contextData.NonDbGameState = nonDbGameState;

            var builders = serviceProvider.GetRequiredService<GameMessageBuilders>();
            builders.SourceChannelBuilder = new DiscordMultiMessageBuilder(new DiscordWebhookBuilder(), () => new DiscordFollowupMessageBuilder());
            builders.GameChannelBuilder = args.Interaction.ChannelId == game.GameChannelId
                ? builders.SourceChannelBuilder
                : DiscordMultiMessageBuilder.Create<DiscordMessageBuilder>();
            builders.PlayerPrivateThreadBuilders = game.Players.ToDictionary(x => x.GamePlayerId,
                _ => DiscordMultiMessageBuilder.Create<DiscordMessageBuilder>());
            var privateThreadPlayer = game.Players.FirstOrDefault(x => x.PrivateThreadId == args.Interaction.ChannelId);
            if (privateThreadPlayer != null)
            {
                builders.PlayerPrivateThreadBuilders[privateThreadPlayer.GamePlayerId] = builders.SourceChannelBuilder;
            }

            var interactionsToSetUp = serviceProvider.GetInteractionsToSetUp();

            var outcome = await Instance.HandleInteractionInternalAsync(builders.GameChannelBuilder, interactionData, game, serviceProvider);

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
                    builders.GameChannelBuilder.AppendContentNewline("ERROR: Failed to save game state. Please report this to the developer.");
                    throw;
                }
            }

            var firstBuilder = builders.SourceChannelBuilder.Builders[0];
            if (firstBuilder is { Components.Count: > 0 })
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
                }

                foreach (var followupBuilder in builders.SourceChannelBuilder.Builders.Skip(1)
                             .Cast<DiscordFollowupMessageBuilder>())
                {
                    await args.Interaction.CreateFollowupMessageAsync(followupBuilder);
                }
            }
            else
            {
                await args.Interaction.DeleteOriginalResponseAsync();
            }

            foreach (var (playerId, playerBuilder) in builders.PlayerPrivateThreadBuilders
                         .Where(x => !x.Value.IsEmpty() && x.Value != builders.SourceChannelBuilder))
            {
                var thread =
                    await GameFlowOperations.GetOrCreatePlayerPrivateThreadAsync(game, game.GetGamePlayerByGameId(playerId));

                foreach (var discordMessageBuilder in playerBuilder.Builders.Cast<DiscordMessageBuilder>()
                             .Where(x => x.Components.Count > 0))
                {
                    await thread.SendMessageAsync(discordMessageBuilder);
                }
            }

            if (builders.GameChannelBuilder != builders.SourceChannelBuilder)
            {
                var gameChannel = await client.GetChannelAsync(game.GameChannelId);
                foreach (var discordMessageBuilder in builders.GameChannelBuilder.Builders.Cast<DiscordMessageBuilder>()
                             .Where(x => x.Components.Count > 0))
                {
                    await gameChannel.SendMessageAsync(discordMessageBuilder);
                }
            }
        }
        catch (Exception e) when (!Program.IsTestEnvironment)
        {
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
    }
}
