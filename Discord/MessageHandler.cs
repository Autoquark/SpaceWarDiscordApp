using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.Discord;

/// <summary>
/// Methods for responding when the bot is mentioned
/// </summary>
public static partial class MessageHandler
{
    private static readonly Regex InteractionIdRegex = MyRegex();
    
    public static async Task HandleMessageCreated(DiscordClient client, MessageCreatedEventArgs args)
    {
        if (args.Message.MentionedUsers.Any(x => x.Id == client.CurrentUser.Id))
        {
            // Check if this is an AI player trying to trigger an interaction
            var match = InteractionIdRegex.Match(args.Message.Content);
            if (match.Success && match.Groups[1].Success)
            {
                if (Guid.TryParse(match.Groups[1].Value, out var interactionId))
                {
                    var interactionData =
                        await Program.FirestoreDb.RunTransactionAsync(transaction =>
                            transaction.GetInteractionDataAsync(interactionId));

                    if (interactionData == null)
                    {
                        return;
                    }

                    var game = await Program.FirestoreDb.RunTransactionAsync(transaction =>
                        transaction.GetGameForChannelAsync(args.Message.ChannelId));

                    if (game == null)
                    {
                        return;
                    }
                    
                    var builder = new DiscordMultiMessageBuilder(new DiscordMessageBuilder(), () => new DiscordMessageBuilder());

                    if (!interactionData.UserAllowedToTrigger(game, args.Author))
                    {
                        // Seems like there is a bug with reply mentions in DSharpPlus so I have to do this to get the mention to work
                        await args.Message.RespondAsync(b => b.AppendContentNewline($"{args.Author.Mention}, looks like you tried to trigger an interaction that is for another player")
                            .WithAllowedMentions(Mentions.All));
                        return;
                    }

                    var serviceProvider = client.ServiceProvider.CreateScope().ServiceProvider;
                    serviceProvider.GetRequiredService<SpaceWarCommandContextData>().GlobalData =
                        await InteractionsHelper.GetGlobalDataAndIncrementInteractionGroupIdAsync();
                    
                    var outcome =
                        await InteractionDispatcher.HandleInteractionAsync(builder, interactionData, game,
                            serviceProvider);

                    if (outcome.RequiresSave)
                    {
                        try
                        {
                            await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));
                        }
                        catch (RpcException)
                        {
                            outcome.SetSimpleReply(
                                "ERROR: Failed to save game state. Please report this to the developer.");
                            throw;
                        }
                    }

                    // Ignore DeleteOriginalMessage for now
                    if (!builder.IsEmpty())
                    {
                        foreach (var innerBuilder in builder.Builders.Cast<DiscordMessageBuilder>())
                        {
                            await args.Message.RespondAsync(innerBuilder);
                        }
                    }
                }
                else
                {
                    // Let the bot know the interaction failed
                    var builder = new DiscordMessageBuilder().EnableV2Components()
                        .WithAllowedMentions(Mentions.All);
                    await args.Message.RespondAsync(builder.AppendContentNewline($"{args.Author.Mention}, looks like you tried to trigger an interaction, but the ID was invalid"));
                }
            }
            else
            {
                // Easter egg: When someone mentions the bot, add a random reaction
                var emoji = new List<DiscordEmoji>
                {
                    Program.CommonEmoji.Cry, Program.CommonEmoji.Heart, Program.CommonEmoji.Laughing,
                    Program.CommonEmoji.OpenMouth, Program.CommonEmoji.StuckOutTongue, Program.CommonEmoji.ThumbsUp
                };
                await args.Message.CreateReactionAsync(emoji.Random());
            }
        }
    }

    [GeneratedRegex(@"\[\[(\S+)\]\]", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}