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
    [Description("Deletes and reuploads all the bot's emojis")]
    public static async Task UpdateEmoji(CommandContext context)
    {
        foreach (var emoji in await context.Client.GetApplicationEmojisAsync())
        {
            await context.Client.DeleteApplicationEmojiAsync(emoji.Id);
        }
        
        var directories = new Queue<string>([DieEmojiDirectoryPath]);
        while (directories.TryDequeue(out var directoryPath))
        {
            foreach (var filePath in Directory.EnumerateFiles(directoryPath))
            {
                await using var stream = File.OpenRead(filePath);
                await context.Client.CreateApplicationEmojiAsync(Path.GetFileNameWithoutExtension(filePath), stream);
            }
            
            foreach (var subDirectory in Directory.EnumerateDirectories(directoryPath))
            {
                directories.Enqueue(subDirectory);
            }
        }

        await context.RespondAsync("Emojis updated! Bot should now be restarted to rebuild emoji ID caches");
    }

    [Command("RegenerateDiceEmoji")]
    [Description("Regenerates the dice emoji images")]
    public static async Task RegenerateDiceEmoji(CommandContext context)
    {
        Directory.CreateDirectory(DieEmojiDirectoryPath);
        foreach (var file in Directory.EnumerateFiles(DieEmojiDirectoryPath, "*.png"))
        {
            File.Delete(file);
        }
        
        foreach (var playerColour in Enum.GetValues<PlayerColour>().Select(PlayerColourInfo.Get))
        {
            var recolorBrush = new RecolorBrush(Color.White, playerColour.ImageSharpColor, 0.5f);
            foreach (var (image, i) in BoardImageGenerator.ColourlessDieIcons.ZipWithIndices())
            {
                using var dieImage = image.Clone(x => x.Fill(recolorBrush));
                await using var fileStream = new FileStream(Path.Combine(DieEmojiDirectoryPath, $"{playerColour.Name}_{i + 1}.png"), FileMode.Create);
                await dieImage.SaveAsPngAsync(fileStream);
            }
            
            using var blankImage = BoardImageGenerator.BlankDieIcon.Clone(x => x.Fill(recolorBrush));
            await using var fileStream2 = new FileStream(Path.Combine(DieEmojiDirectoryPath, $"{playerColour.Name}_blank.png"), FileMode.Create);
            await blankImage.SaveAsPngAsync(fileStream2);
        }
        
        await context.RespondAsync("Emojis regenerated!");
    }

    [Command("ShowGameId")]
    [RequireGameChannel(RequireGameChannelMode.ReadOnly)]
    public static async Task ShowGameId(CommandContext context)
    {
        var game = context.ServiceProvider.GetRequiredService<SpaceWarCommandContextData>().Game!;
        var outcome = context.Outcome();

        outcome.ReplyBuilder = DiscordMultiMessageBuilder.Create<DiscordMessageBuilder>()
            .AppendContentNewline($"Game document ID: {game.DocumentId}");
    }

    [Command("NewChannelForGame")]
    public static async Task NewChannelForGame(CommandContext context, string gameId)
    {
        var game = await Program.FirestoreDb.RunTransactionAsync(transaction =>
            transaction.GetGameAsync(transaction.Database.Games().Document(gameId)));

        var outcome = context.Outcome();
        outcome.ReplyBuilder = DiscordMultiMessageBuilder.Create<DiscordMessageBuilder>();
        if (game == null)
        {
            outcome.ReplyBuilder.AppendContentNewline($"Game not found");
        }
        else
        {
            var channel = await context.Guild!.CreateChannelAsync(game.Name, DiscordChannelType.Text);
            game.GameChannelId = channel.Id;
            await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));
            
            outcome.ReplyBuilder.AppendContentNewline($"Created channel {channel.Mention}");
        }
    }
    
}