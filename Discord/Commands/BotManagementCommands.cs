using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord.ContextChecks;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;
using SpaceWarDiscordApp.ImageGeneration;

namespace SpaceWarDiscordApp.Discord.Commands;

[RequireApplicationOwner]
public class BotManagementCommands
{
    private const string DieEmojiDirectoryPath = "./Icons/Emoji/Dice";

    [Command("UpdateEmoji")]
    [Description("Deletes and reuploads all the bot's emojis and regenerates player coloured die emoji")]
    public static async Task UpdateEmoji(CommandContext context)
    {
        await BotManagementOperations.UpdateEmojiAsync();
        await context.RespondAsync("Emojis updated!");
    }

    [Command("ShowGameId")]
    [RequireGameChannel(RequireGameChannelMode.ReadOnly)]
    public static async Task ShowGameId(CommandContext context)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;

        context.ServiceProvider.GetRequiredService<GameMessageBuilders>().SourceChannelBuilder
            .AppendContentNewline($"Game document ID: {game.DocumentId}");
    }

    [Command("NewChannelForGame")]
    public static async Task NewChannelForGame(CommandContext context, string gameId)
    {
        var game = await Program.FirestoreDb.RunTransactionAsync(transaction =>
            transaction.GetGameAsync(transaction.Database.Games().Document(gameId)));

        var builder = context.ServiceProvider.GetRequiredService<GameMessageBuilders>().SourceChannelBuilder;
        
        if (game == null)
        {
            builder.AppendContentNewline("Game not found");
            return;
        }

        var channel = await context.Guild!.CreateChannelAsync(game.Name, DiscordChannelType.Text);
        game.GameChannelId = channel.Id;
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));

        builder.AppendContentNewline($"Created new channel {channel.Mention} for existing game");
    }

    [Command("ClearGameCache")]
    public static async Task ClearGameCache(CommandContext context, string? gameId = null)
    {
        var cache = context.ServiceProvider.GetRequiredService<GameCache>();
        var builder = context.ServiceProvider.GetRequiredService<GameMessageBuilders>().SourceChannelBuilder;
        
        if (gameId != null)
        {
            var documentReference = Program.FirestoreDb.Games().Document(gameId);
            cache.Clear(documentReference);
            builder.AppendContentNewline($"Cache cleared for {documentReference}");
        }
        else
        {
            var channelGame = await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.GetGameForChannelAsync(context.Channel));
            if (channelGame == null)
            {
                builder.AppendContentNewline("No game id given or game found for this channel");
                return;
            }
            
            cache.Clear(channelGame);
            builder.AppendContentNewline($"Cache cleared for {channelGame.DocumentId}");
        }
    }

    [Command("ClearAllGameCache")]
    public static async Task ClearAllGameCache(CommandContext context)
    {
        var cache = context.ServiceProvider.GetRequiredService<GameCache>();
        var outcome = context.Outcome();
        cache.ClearAll();
        
        context.ServiceProvider.GetRequiredService<GameMessageBuilders>().SourceChannelBuilder
            .AppendContentNewline("Cache cleared for all games");
    }
}